namespace Api.OpenFGA;

public sealed record OpenFgaConfig
{
	public const string Section = "OpenFGA";

	public required Uri Host { get; set; }
	public required string StoreId { get; set; }
	public required string ModelId { get; set; }
}