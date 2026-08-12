using Api.Auth;

using Auth0;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Account.Endpoint;

/// <summary>Self-service endpoints for the current user to update their own Auth0 profile.</summary>
public static class Account
{
	/// <summary>Maps the account endpoints into a route group.</summary>
	public static void MapAccountEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapPatch("/name", UpdateName);
		builder.MapPatch("/email", UpdateEmail);
		builder.MapPost("/email/verify", SendVerificationEmail);
	}

	[EndpointName("UpdateAccountName")]
	static async Task<Ok> UpdateName(
		UpdateNameRequest body,
		[FromServices] ICurrentUser currentUser,
		[FromServices] Auth0ApiClient auth0,
		CancellationToken cancellationToken)
	{
		await auth0.UpdateName(currentUser.Id, body.Name, cancellationToken);
		return TypedResults.Ok();
	}

	/// <summary>Updates the current user's email. Auth0 resets email_verified to false and sends
	/// its own verification email as a side effect.</summary>
	[EndpointName("UpdateAccountEmail")]
	static async Task<Ok> UpdateEmail(
		UpdateEmailRequest body,
		[FromServices] ICurrentUser currentUser,
		[FromServices] Auth0ApiClient auth0,
		CancellationToken cancellationToken)
	{
		await auth0.UpdateEmail(currentUser.Id, body.Email, cancellationToken);
		return TypedResults.Ok();
	}

	[EndpointName("SendAccountVerificationEmail")]
	static async Task<Ok> SendVerificationEmail(
		[FromServices] ICurrentUser currentUser,
		[FromServices] Auth0ApiClient auth0,
		CancellationToken cancellationToken)
	{
		await auth0.SendVerificationEmail(currentUser.Id, cancellationToken);
		return TypedResults.Ok();
	}
}

public sealed record UpdateNameRequest
{
	public required string Name { get; init; }
}

public sealed record UpdateEmailRequest
{
	public required string Email { get; init; }
}