namespace Auth0;

public sealed record Auth0Config
{
	public const string Section = "Auth0";

	/// <summary>Auth0 tenant domain.</summary>
	/// <example>https://your-tenant.eu.auth0.com</example>
	public required Uri Domain { get; set; }

	/// <summary>Client id of the Machine-to-Machine application authorized for the Management
	/// API, granted the `update:users` and `create:user_tickets` scopes.</summary>
	public required string ManagementClientId { get; set; }

	/// <summary>Client secret of the Machine-to-Machine application.</summary>
	public required string ManagementClientSecret { get; set; }

	/// <summary>Name of the database connection users authenticate against. The Management API
	/// requires this when changing a user's email.</summary>
	public string Connection { get; set; } = "Username-Password-Authentication";
}