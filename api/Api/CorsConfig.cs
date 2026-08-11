namespace Api.Config;

/// <summary>Configuration for enabling CORS for specific origins.</summary>
public sealed record CorsConfig
{
	/// <summary>Key to use in appsettings.json</summary>
	public const string SectionKey = "Cors";

	/// <summary>Origins to allow cross-origin requests from. Leave empty to disable CORS (Development still allows any origin regardless).</summary>
	/// <example>["https://example.com"]</example>
	public string[] AllowedOrigins { get; init; } = [];
}
