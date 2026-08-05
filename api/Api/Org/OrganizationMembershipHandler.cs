using Api.Org.Model;
using Api.Org.Model.Events;

using Api.OpenFGA;

namespace Api.Org;

/// <summary>Keeps OpenFGA tuples in sync with organization membership events. Wolverine
/// discovers these Handle methods by convention.</summary>
public static class OrganizationMembershipHandler
{
	public static Task Handle(OrganizationMemberAdded e, OpenFgaApiClient fga) =>
		fga.Write(e.UserId, e.Role.ToFgaRelation(), "organization", e.Id.ToString());

	public static Task Handle(OrganizationMemberRemoved e, OpenFgaApiClient fga) =>
		fga.Write(e.UserId, e.Role.ToFgaRelation(), "organization", e.Id.ToString(), delete: true);
}
