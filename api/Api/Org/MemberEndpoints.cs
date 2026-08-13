using Api.Auth;
using Api.OpenFGA;
using Api.Org.Model.Events;
using Api.Org.Response;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Wolverine.Marten;

namespace Api.Org.Endpoint;

/// <summary>Represents self-service organization endpoints: creating an organization as any
/// authenticated user, listing the organizations the current user belongs to, and managing
/// membership on organizations the current user administers.</summary>
public static class OrganizationMembers
{
	/// <summary>Maps the organization membership endpoints into a route group.</summary>
	public static void MapOrganizationMemberEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapGet("/", GetMyOrganizations);
		builder.MapPost("/", CreateOrganization);
		builder.MapGet("/{id:guid}", GetOrganization)
			.RequireAuthorization(new OpenFgaAuthorizationRequirement("can_view", "organization"));
		builder.MapPost("/{id:guid}/member", AddMember)
			.RequireAuthorization(new OpenFgaAuthorizationRequirement("can_add", "organization"));
		builder.MapDelete("/{id:guid}/member/{userId}", RemoveMember)
			.RequireAuthorization(new OpenFgaAuthorizationRequirement("can_add", "organization"));
		builder.MapPost("/{id:guid}/leave", LeaveOrganization);
		builder.MapPost("/{id:guid}/invite", CreateInvite)
			.RequireAuthorization(new OpenFgaAuthorizationRequirement("can_add", "organization"));
	}

	static async Task<Ok<IReadOnlyList<OrganizationMembership>>> GetMyOrganizations(
		[FromServices] IQuerySession session,
		[FromServices] ICurrentUser currentUser)
	{
		var organizations = await session.Query<Model.Organization>()
			.Where(o => o.Members.ContainsKey(currentUser.Id))
			.ToListAsync();

		IReadOnlyList<OrganizationMembership> result = [.. organizations.Select(o => new OrganizationMembership
		{
			Id = o.Id,
			Name = o.Name,
			Role = o.Members[currentUser.Id],
		})];

		return TypedResults.Ok(result);
	}

	static async Task<Results<Ok<OrganizationDetails>, NotFound>> GetOrganization(
		Guid id,
		[FromServices] IDocumentSession session)
	{
		// FetchLatest reads directly off the event stream rather than the async-projected
		// snapshot doc, so a just-created organization is visible immediately.
		var org = await session.Events.FetchLatest<Model.Organization>(id);
		if (org is null)
		{
			return TypedResults.NotFound();
		}

		return TypedResults.Ok(new OrganizationDetails
		{
			Id = org.Id,
			Name = org.Name,
			Members = [.. org.Members.Select(m => new OrganizationMemberInfo { UserId = m.Key, Role = m.Value })],
		});
	}

	static async Task<Created<Organization>> CreateOrganization(
		[FromServices] IDocumentSession session,
		[FromServices] IMartenOutbox outbox,
		[FromServices] ICurrentUser currentUser,
		CreateOrganizationRequest body)
	{
		var org = new Organization
		{
			Id = Guid.NewGuid(),
			Name = body.Name,
		};

		outbox.Enroll(session);

		var memberAdded = new OrganizationMemberAdded(org.Id, currentUser.Id, Model.OrganizationRole.Admin);
		session.Events.StartStream<Model.Organization>(org.Id, new OrganizationCreated(org.Id, org.Name), memberAdded);
		await outbox.PublishAsync(memberAdded);
		await session.SaveChangesAsync();

		return TypedResults.Created($"/organization/{org.Id}", org);
	}

	static async Task<Results<Ok, NotFound>> AddMember(
		Guid id,
		AddMemberRequest body,
		[FromServices] IDocumentSession session,
		[FromServices] IMartenOutbox outbox)
	{
		outbox.Enroll(session);

		var stream = await session.Events.FetchForWriting<Model.Organization>(id);
		if (stream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		var memberAdded = new OrganizationMemberAdded(id, body.UserId, body.Role);
		stream.AppendOne(memberAdded);
		await outbox.PublishAsync(memberAdded);
		await session.SaveChangesAsync();

		return TypedResults.Ok();
	}

	/// <summary>Removes a member from an organization. Idempotent: responds 200 whether the user was
	/// actually removed by this call or was already not a member.</summary>
	static async Task<Results<Ok, NotFound>> RemoveMember(
		Guid id,
		string userId,
		[FromServices] IDocumentSession session,
		[FromServices] IMartenOutbox outbox)
	{
		outbox.Enroll(session);

		var stream = await session.Events.FetchForWriting<Model.Organization>(id);
		if (stream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		if (stream.Aggregate.Members.TryGetValue(userId, out var role))
		{
			var memberRemoved = new OrganizationMemberRemoved(id, userId, role);
			stream.AppendOne(memberRemoved);
			await outbox.PublishAsync(memberRemoved);
			await session.SaveChangesAsync();
		}

		return TypedResults.Ok();
	}

	/// <summary>Removes the current user from an organization. Only requires that the caller is
	/// currently a member (no can_add relation needed, unlike AddMember/RemoveMember). If the
	/// caller is the last member, the organization is deleted along with them.</summary>
	static async Task<Results<Ok, NotFound, ForbidHttpResult>> LeaveOrganization(
		Guid id,
		[FromServices] IDocumentSession session,
		[FromServices] IMartenOutbox outbox,
		[FromServices] ICurrentUser currentUser)
	{
		outbox.Enroll(session);

		var stream = await session.Events.FetchForWriting<Model.Organization>(id);
		if (stream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		if (!stream.Aggregate.Members.TryGetValue(currentUser.Id, out var role))
		{
			return TypedResults.Forbid();
		}

		var memberRemoved = new OrganizationMemberRemoved(id, currentUser.Id, role);
		if (stream.Aggregate.Members.Count == 1)
		{
			stream.AppendMany(memberRemoved, new OrganizationDeleted(id));
		}
		else
		{
			stream.AppendOne(memberRemoved);
		}

		await outbox.PublishAsync(memberRemoved);
		await session.SaveChangesAsync();

		return TypedResults.Ok();
	}

	/// <summary>Creates an invite for the given email to join an organization, and returns it with
	/// its id, which doubles as the join token embedded in the invite link. No email is sent yet;
	/// the caller is responsible for sharing the link.</summary>
	static async Task<Results<Created<OrganizationInvite>, NotFound>> CreateInvite(
		Guid id,
		CreateInviteRequest body,
		[FromServices] IDocumentSession session)
	{
		var org = await session.Events.FetchLatest<Model.Organization>(id);
		if (org is null)
		{
			return TypedResults.NotFound();
		}

		var invite = new Model.OrganizationInvite
		{
			Id = Guid.NewGuid(),
			OrganizationId = id,
			Email = body.Email,
		};
		session.Events.StartStream<Model.OrganizationInvite>(
			invite.Id,
			new OrganizationInviteCreated(invite.Id, invite.OrganizationId, invite.Email));
		await session.SaveChangesAsync();

		return TypedResults.Created(
			$"/organization/{id}/invite/{invite.Id}",
			new OrganizationInvite { Id = invite.Id, OrganizationId = invite.OrganizationId, Email = invite.Email });
	}
}

public sealed record AddMemberRequest
{
	public required string UserId { get; init; }

	public required Model.OrganizationRole Role { get; init; }
}

public sealed record CreateInviteRequest
{
	public required string Email { get; init; }
}
