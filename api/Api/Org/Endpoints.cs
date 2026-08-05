using Api.Common;
using Api.Org.Model.Events;
using Api.Org.Response;

using Marten;
using Marten.Pagination;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Org.Endpoint;

/// <summary>Represents organizations endpoints.</summary>
public static class Organizations
{
	const int MaxPageSize = 100;

	/// <summary>Maps the organization endpoints into a route group.</summary>
	public static void MapOrganizationEndpoints(this RouteGroupBuilder builder)
	{
		builder.MapGet("/", GetOrganizations);
		builder.MapPost("/", CreateOrganization);
		builder.MapPatch("/{id:guid}", UpdateOrganization);
		builder.MapDelete("/{id:guid}", DeleteOrganization);
	}

	static async Task<Results<Ok<PagedResponse<Organization>>, ValidationProblem>> GetOrganizations(
		[AsParameters] ListOrganizationsRequest query,
		[FromServices] IQuerySession session)
	{
		var page = query.Page ?? 1;
		var pageSize = query.PageSize ?? 20;
		var sortDescending = query.SortDescending ?? false;

		var errors = new Dictionary<string, string[]>();
		if (page < 1)
		{
			errors["page"] = ["Page must be 1 or greater."];
		}

		if (pageSize is < 1 or > MaxPageSize)
		{
			errors["pageSize"] = [$"PageSize must be between 1 and {MaxPageSize}."];
		}

		if (errors.Count > 0)
		{
			return TypedResults.ValidationProblem(errors);
		}

		IQueryable<Model.Organization> organizations = session.Query<Model.Organization>();
		if (!string.IsNullOrWhiteSpace(query.Search))
		{
			organizations = organizations.Where(o => o.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
		}

		var sorted = sortDescending
			? organizations.OrderByDescending(o => o.Name).ThenBy(o => o.Id)
			: organizations.OrderBy(o => o.Name).ThenBy(o => o.Id);

		var pagedResult = await sorted.ToPagedListAsync(page, pageSize);

		return TypedResults.Ok(new PagedResponse<Organization>
		{
			Data = [.. pagedResult.Select(o => new Organization { Id = o.Id, Name = o.Name })],
			Page = pagedResult.PageNumber,
			PageSize = pagedResult.PageSize,
			Total = pagedResult.TotalItemCount,
			PageCount = pagedResult.PageCount,
		});
	}

	static async Task<Results<Created<Organization>, UnprocessableEntity>>
	CreateOrganization(
		[FromServices] IDocumentSession session,
		CreateOrganizationRequest body)
	{
		var org = new Organization
		{
			Id = Guid.NewGuid(),
			Name = body.Name,
		};
		session.Events.StartStream<Model.Organization>(org.Id, new OrganizationCreated(org.Id, org.Name));
		await session.SaveChangesAsync();

		return TypedResults.Created($"/organization/{org.Id}", org);
	}

	static async Task<Results<Ok<Organization>, NotFound>> UpdateOrganization(
		Guid id,
		UpdateOrganizationRequest body,
		[FromServices] IDocumentSession session)
	{
		var stream = await session.Events.FetchForWriting<Model.Organization>(id);
		if (stream.Aggregate is null)
		{
			return TypedResults.NotFound();
		}

		stream.AppendOne(new OrganizationUpdated(id, body.Name));
		await session.SaveChangesAsync();

		return TypedResults.Ok(new Organization { Id = id, Name = body.Name });
	}

	/// <summary>Deletes an organization. Idempotent: responds 200 whether an organization was actually
	/// deleted by this call or was already gone (never existed, or already deleted).</summary>
	static async Task<Ok> DeleteOrganization(
		Guid id,
		[FromServices] IDocumentSession session)
	{
		var stream = await session.Events.FetchForWriting<Model.Organization>(id);
		if (stream.Aggregate is not null)
		{
			stream.AppendOne(new OrganizationDeleted(id));
			await session.SaveChangesAsync();
		}

		return TypedResults.Ok();
	}
}

public sealed record CreateOrganizationRequest
{
	public required string Name { get; init; }
}

public sealed record UpdateOrganizationRequest
{
	public required string Name { get; init; }
}

/// <summary>Query parameters for listing organizations, page-based and sortable by name.
/// All properties are optional in the query string; unset values fall back to their defaults
/// (page 1, page size 20, ascending, unfiltered) in the handler.</summary>
public sealed record ListOrganizationsRequest
{
	public int? Page { get; init; }

	public int? PageSize { get; init; }

	public bool? SortDescending { get; init; }

	/// <summary>Case-insensitive substring match against the organization name. Unset or blank means no filtering.</summary>
	public string? Search { get; init; }
}