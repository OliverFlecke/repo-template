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

public sealed record OrganizationDetails
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required IReadOnlyList<OrganizationMemberInfo> Members { get; init; }
}

public sealed record OrganizationMemberInfo
{
	public required string UserId { get; init; }

	/// <summary>The member's Auth0 display name, or null if Auth0 has no such user (e.g. deleted
	/// since joining) or the lookup failed.</summary>
	public string? Name { get; init; }

	public required OrganizationRole Role { get; init; }
}

public sealed record OrganizationInvite
{
	public required Guid Id { get; init; }

	public required Guid OrganizationId { get; init; }

	public required string Email { get; init; }
}

public sealed record InviteDetails
{
	public required Guid OrganizationId { get; init; }

	public required string OrganizationName { get; init; }

	public required string Email { get; init; }

	public required bool Accepted { get; init; }
}