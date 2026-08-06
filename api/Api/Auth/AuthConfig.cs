namespace Api.Auth;

/// <summary>Configuration for validating incoming Bearer tokens.</summary>
public sealed record AuthConfig
{
	/// <summary>Key to use in appsettings.json</summary>
	public const string SectionKey = "Auth";

	/// <summary>The issuer authority to validate tokens against and discover signing keys from (its `/.well-known/openid-configuration` endpoint).</summary>
	/// <example>https://login.example.com/realms/example</example>
	public required string Authority { get; init; }

	/// <summary>The expected `aud` claim on incoming tokens. Left unset, the audience is not validated.</summary>
	public required string Audience { get; init; }

	/// <summary>Whether the authority's metadata and token endpoints must be served over HTTPS. Should only be disabled for local development.</summary>
	public bool RequireHttpsMetadata { get; init; } = true;
}