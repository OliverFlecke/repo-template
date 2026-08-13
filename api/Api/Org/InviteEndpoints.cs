using Api.Auth;
using Api.Org.Model.Events;
using Api.Org.Response;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using Wolverine.Marten;

namespace Api.Org.Endpoint;

/// <summary>Represents invite endpoints: looking up an invite by its token (public, so the join
/// page can show the organization's name before the visitor signs in) and accepting one.</summary>
public static class Invites
{
	/// <summary>Maps the invite endpoints into a route group.</summary>
	public static void MapInviteEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapGet("/{token:guid}", GetInvite).AllowAnonymous();
		builder.MapPost("/{token:guid}/accept", AcceptInvite);
	}

	static async Task<Results<Ok<InviteDetails>, NotFound>> GetInvite(
		Guid token,
		[FromServices] IDocumentSession session)
	{
		var invite = await session.Events.FetchLatest<Model.OrganizationInvite>(token);
		if (invite is null)
		{
			return TypedResults.NotFound();
		}

		var org = await session.Events.FetchLatest<Model.Organization>(invite.OrganizationId);
		if (org is null)
		{
			return TypedResults.NotFound();
		}

		return TypedResults.Ok(new InviteDetails
		{
			OrganizationId = org.Id,
			OrganizationName = org.Name,
			Email = invite.Email,
			Accepted = invite.AcceptedByUserId is not null,
		});
	}

	/// <summary>Accepts an invite, adding the caller as a member of the invite's organization.
	/// Holding a valid, unused token is itself the authorization to join - no separate OpenFGA
	/// relation is required.</summary>
	static async Task<Results<Ok, NotFound, Conflict>> AcceptInvite(
		Guid token,
		[FromServices] IDocumentSession session,
		[FromServices] IMartenOutbox outbox,
		[FromServices] ICurrentUser currentUser)
	{
		outbox.Enroll(session);

		var inviteStream = await session.Events.FetchForWriting<Model.OrganizationInvite>(token);
		if (inviteStream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		if (inviteStream.Aggregate.AcceptedByUserId is not null)
		{
			return TypedResults.Conflict();
		}

		var orgStream = await session.Events.FetchForWriting<Model.Organization>(inviteStream.Aggregate.OrganizationId);
		if (orgStream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		var memberAdded = new OrganizationMemberAdded(orgStream.Aggregate.Id, currentUser.Id, Model.OrganizationRole.Member);
		orgStream.AppendOne(memberAdded);
		inviteStream.AppendOne(new OrganizationInviteAccepted(token, currentUser.Id));
		await outbox.PublishAsync(memberAdded);
		await session.SaveChangesAsync();

		return TypedResults.Ok();
	}
}
