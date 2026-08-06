using Api.Org.Model.Events;

namespace Api.Org.Model;

public sealed record Organization
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	// Concrete Dictionary rather than IReadOnlyDictionary: Marten's Linq provider only recognizes
	// Dictionary<,>.ContainsKey when translating queries to SQL, not the interface method.
	public Dictionary<string, OrganizationRole> Members { get; init; } = [];

	public static Organization Create(OrganizationCreated e) => new()
	{
		Id = e.Id,
		Name = e.Name,
	};

	public static Organization Apply(OrganizationUpdated e, Organization current) => current with
	{
		Name = e.Name,
	};

	public static Organization Apply(OrganizationMemberAdded e, Organization current) => current with
	{
		Members = new Dictionary<string, OrganizationRole>(current.Members) { [e.UserId] = e.Role },
	};

	public static Organization Apply(OrganizationMemberRemoved e, Organization current)
	{
		var members = new Dictionary<string, OrganizationRole>(current.Members);
		members.Remove(e.UserId);
		return current with { Members = members };
	}

	public static bool ShouldDelete(OrganizationDeleted _) => true;
}