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
		builder.MapPost("/{id:guid}/member", AddMember)
			.RequireAuthorization(new OpenFgaAuthorizationRequirement("can_add", "organization"));
		builder.MapDelete("/{id:guid}/member/{userId}", RemoveMember)
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
}

public sealed record AddMemberRequest
{
	public required string UserId { get; init; }

	public required Model.OrganizationRole Role { get; init; }
}
