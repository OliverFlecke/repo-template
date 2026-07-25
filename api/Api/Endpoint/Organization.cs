using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoint;

/// <summary>Represents organizations endpoints.</summary>
public static class Organizations
{
	static readonly List<Organization> Samples = [
		new Organization { Name = "Apple" },
		new Organization { Name = "Google" },
		new Organization { Name = "Microsoft" },
	];

	/// <summary>Maps the organization endpoints into a route group.</summary>
	public static void MapOrganizationEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapGet("/", GetOrganizations);
		builder.MapPost("/{id}", CreateOrganization);
		builder.MapGet("/{id}", GetOrganization);
	}

	static Results<Ok<List<Organization>>, NotFound> GetOrganizations() => TypedResults.Ok(Samples);

	static Results<Ok<Organization>, NotFound> GetOrganization(string id) =>
		Samples.FirstOrDefault(o => o.Name == id) is Organization org
		? TypedResults.Ok(org)
		: TypedResults.NotFound();

	static Results<Created<Organization>, UnprocessableEntity> CreateOrganization(string id)
	{
		var org = new Organization { Name = id };
		Samples.Add(org);

		return TypedResults.Created($"/organization/{id}", org);
	}
}

/// <summary>
/// Represents an organization which can contains members and other organizations.
/// </summary>
public sealed record Organization
{
	/// <summary>The name of the organization.</summary>
	/// <example>Apple</example>
	public required string Name { get; init; }
}