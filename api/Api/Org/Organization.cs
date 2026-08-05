using Api.Org.Model.Events;

namespace Api.Org.Model;

public sealed record Organization
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public static Organization Create(OrganizationCreated e) => new()
	{
		Id = e.Id,
		Name = e.Name,
	};

	public static Organization Apply(OrganizationUpdated e, Organization current) => current with
	{
		Name = e.Name,
	};

	public static bool ShouldDelete(OrganizationDeleted _) => true;
}