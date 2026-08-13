using Api.Org.Model.Events;

namespace Api.Org.Model;

/// <summary>An invitation for a given email to join an organization. The invite's <see cref="Id"/>
/// doubles as the join token embedded in the invite link.</summary>
public sealed record OrganizationInvite
{
	public required Guid Id { get; init; }

	public required Guid OrganizationId { get; init; }

	public required string Email { get; init; }

	/// <summary>The id of the user who accepted this invite, or null if it hasn't been accepted yet.
	/// Once set, the invite can no longer be accepted again.</summary>
	public string? AcceptedByUserId { get; init; }

	public static OrganizationInvite Create(OrganizationInviteCreated e) => new()
	{
		Id = e.Id,
		OrganizationId = e.OrganizationId,
		Email = e.Email,
	};

	public static OrganizationInvite Apply(OrganizationInviteAccepted e, OrganizationInvite current) => current with
	{
		AcceptedByUserId = e.UserId,
	};
}
