namespace Api.Org.Response;

public sealed record Organization
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }
}