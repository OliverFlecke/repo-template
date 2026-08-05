using System.Text.Json.Serialization;

namespace Api.Org.Model;

/// <summary>A member's role within an organization. Maps to the OpenFGA "member"/"admin"
/// relations on the `organization` type (see libs/OpenFGA/core.fga) via <see cref="OrganizationRoleExtensions.ToFgaRelation"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrganizationRole>))]
public enum OrganizationRole
{
	Member,
	Admin,
}

public static class OrganizationRoleExtensions
{
	public static string ToFgaRelation(this OrganizationRole role) => role switch
	{
		OrganizationRole.Member => "member",
		OrganizationRole.Admin => "admin",
		_ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
	};
}
