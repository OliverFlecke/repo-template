using Api.Org.Model.Events;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Org.Endpoint;

/// <summary>Represents organizations endpoints.</summary>
public static class Organizations
{
	/// <summary>Maps the organization endpoints into a route group.</summary>
	public static void MapOrganizationEndpoints(this RouteGroupBuilder builder)
	{
		// builder.MapGet("/", GetOrganizations);
		// builder.MapGet("/{id}", GetOrganization);
		builder.MapPost("/", CreateOrganization);
	}

	// static Results<Ok<List<Organization>>, NotFound> GetOrganizations()
	// {
	// 	return TypedResults.Ok(Samples);
	// }
	//
	// static Results<Ok<Organization>, NotFound> GetOrganization(string id) =>
	// 	Samples.FirstOrDefault(o => o.Name == id) is Organization org
	// 	? TypedResults.Ok(org)
	// 	: TypedResults.NotFound();

	static async Task<Results<Created<Response.Organization>, UnprocessableEntity>>
	CreateOrganization(
		[FromServices] IDocumentSession session,
		CreateOrganizationRequest body)
	{
		var org = new Response.Organization
		{
			Id = Guid.NewGuid(),
			Name = body.Name,
		};
		session.Events.StartStream<Model.Organization>(org.Id, new OrganizationCreated(org.Id, org.Name));
		await session.SaveChangesAsync();

		return TypedResults.Created($"/organization/{org.Id}", org);
	}
}

public sealed record CreateOrganizationRequest
{
	public required string Name { get; init; }
}