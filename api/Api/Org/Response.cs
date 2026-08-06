using Api.Org.Model;

namespace Api.Org.Response;

public sealed record Organization
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }
}

public sealed record OrganizationMembership
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required OrganizationRole Role { get; init; }
}