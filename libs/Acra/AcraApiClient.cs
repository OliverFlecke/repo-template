using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Acra;

public sealed class AcraApiClient(
	ILogger<AcraApiClient> logger,
	AcraConfig config,
	HttpClient http
)
{
	static readonly JsonSerializerOptions Json = new();

	static AcraApiClient()
	{
		Json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
		Json.TypeInfoResolverChain.Insert(0, AcraJsonSerializerContext.Default);
	}

	/// <summary>Looks up a single entity by its exact UEN.</summary>
	/// <param name="uen">The Unique Entity Number to search for.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The matching entity, or null if no entity has that UEN.</returns>
	public async Task<AcraEntity?> SearchByUen(string uen, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Searching ACRA for UEN {Uen}", uen);

		var result = await ExecuteSearch(new()
		{
			["resource_id"] = config.ResourceId,
			["filters"] = new JsonObject { ["uen"] = uen }.ToJsonString(),
			["limit"] = "1",
		}, cancellationToken);

		return result.Records.Count > 0 ? result.Records[0] : null;
	}

	/// <summary>Searches entities by (partial) company name.</summary>
	/// <param name="name">The company name text to search for.</param>
	/// <param name="limit">Maximum number of rows to return.</param>
	/// <param name="offset">Number of rows to skip, for paging.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public async Task<AcraSearchResult> SearchByName(
		string name,
		int limit = 100,
		int offset = 0,
		CancellationToken cancellationToken = default
	)
	{
		logger.LogInformation("Searching ACRA for entity name {Name}", name);

		var result = await ExecuteSearch(new()
		{
			["resource_id"] = config.ResourceId,
			["q"] = new JsonObject { ["entity_name"] = name }.ToJsonString(),
			["limit"] = limit.ToString(),
			["offset"] = offset.ToString(),
		}, cancellationToken);

		return new AcraSearchResult { Records = result.Records, Total = result.Total };
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	async Task<CkanResult> ExecuteSearch(Dictionary<string, string?> query, CancellationToken cancellationToken)
	{
		var url = QueryHelpers.AddQueryString("/api/action/datastore_search", query);
		var response = await http.GetAsync(url, cancellationToken);
		response.EnsureSuccessStatusCode();

		var envelope = await response.Content.ReadFromJsonAsync<CkanEnvelope>(Json, cancellationToken);
		if (envelope is not { Success: true, Result: { } result })
		{
			throw new AcraApiException(envelope?.Error?.Message ?? "ACRA API returned an unsuccessful response");
		}

		return result;
	}
}

public sealed record AcraEntity
{
	public required string Uen { get; init; }
	public required string EntityName { get; init; }
	public string? IssuanceAgencyDesc { get; init; }
	public string? UenStatusDesc { get; init; }
	public string? EntityTypeDesc { get; init; }
	public string? UenIssueDate { get; init; }
	public string? RegStreetName { get; init; }
	public string? RegPostalCode { get; init; }
}

public sealed record AcraSearchResult
{
	public required IReadOnlyList<AcraEntity> Records { get; init; }

	/// <summary>Total number of matching rows in the dataset, which may exceed Records.Count when paging.</summary>
	public required int Total { get; init; }
}

public sealed class AcraApiException(string message) : Exception(message);

internal sealed record CkanEnvelope
{
	public bool Success { get; init; }
	public CkanResult? Result { get; init; }
	public CkanError? Error { get; init; }
}

internal sealed record CkanResult
{
	public required IReadOnlyList<AcraEntity> Records { get; init; }
	public required int Total { get; init; }
}

internal sealed record CkanError
{
	public string? Message { get; init; }
}

[JsonSerializable(typeof(CkanEnvelope))]
[JsonSerializable(typeof(CkanResult))]
[JsonSerializable(typeof(CkanError))]
[JsonSerializable(typeof(AcraEntity))]
partial class AcraJsonSerializerContext : JsonSerializerContext { }
