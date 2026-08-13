using Api.Auth;

using Auth0;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Account.Endpoint;

/// <summary>Self-service endpoints for the current user to update their own Auth0 profile.</summary>
public static class Account
{
	/// <summary>
	/// Users on Auth0's database connection have a `sub` claim prefixed like this;
	/// social/enterprise connections (google-oauth2|..., etc.) don't have an Auth0-managed
	/// password to change.
	/// </summary>
	const string DatabaseConnectionSubjectPrefix = "auth0|";

	/// <summary>Maps the account endpoints into a route group.</summary>
	public static void MapAccountEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapPatch("/name", UpdateName);
		builder.MapPatch("/email", UpdateEmail);
		builder.MapPost("/email/verify", SendVerificationEmail);
		builder.MapPatch("/password", UpdatePassword);
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

	/// <summary>
	/// Updates the current user's email. Sets verify_email so Auth0 resets email_verified to
	/// false and sends a fresh verification email itself, as part of this same call.
	/// </summary>
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

	/// <summary>
	/// Updates the current user's password. Only available for users on Auth0's database
	/// connection.
	/// </summary>
	[EndpointName("UpdateAccountPassword")]
	static async Task<Results<Ok, ForbidHttpResult>> UpdatePassword(
		UpdatePasswordRequest body,
		[FromServices] ICurrentUser currentUser,
		[FromServices] Auth0ApiClient auth0,
		CancellationToken cancellationToken)
	{
		if (!currentUser.Id.StartsWith(DatabaseConnectionSubjectPrefix, StringComparison.Ordinal))
		{
			return TypedResults.Forbid();
		}

		await auth0.UpdatePassword(currentUser.Id, body.Password, cancellationToken);
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

public sealed record UpdatePasswordRequest
{
	public required string Password { get; init; }
}