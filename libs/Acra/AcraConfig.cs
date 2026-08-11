namespace Acra;

public sealed record AcraConfig
{
	public const string Section = "Acra";
	public const string DefaultResourceId = "d_3f960c10fed6145404ca7b821f263b87";

	/// <summary>data.gov.sg host. Defaults to production; use https://api-public-staging.data.gov.sg for staging.</summary>
	public Uri Host { get; set; } = new("https://data.gov.sg");

	/// <summary>The datastore_search resource id for the ACRA "Entities Registered with ACRA" dataset.</summary>
	public string ResourceId { get; set; } = DefaultResourceId;

	/// <summary>Optional API key sent as the x-api-key header for higher rate limits. Not required.</summary>
	public string? ApiKey { get; set; }
}
