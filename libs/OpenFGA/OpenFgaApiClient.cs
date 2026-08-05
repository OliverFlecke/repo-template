using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace Api.OpenFGA;

public sealed class OpenFgaApiClient(
	ILogger<OpenFgaApiClient> logger,
	OpenFgaConfig config,
	HttpClient http
)
{
	static readonly JsonSerializerOptions Json = new();

	static OpenFgaApiClient()
	{
		Json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
		Json.TypeInfoResolverChain.Insert(0, FgaClientJsonSerializerContext.Default);
	}

	/// <summary>
	/// Check authorization for a user with a relationship on an object.
	/// </summary>
	/// <param name="user">The user to check authorization for.</param>
	/// <param name="relation">The relationship to check.</param>
	/// <param name="objectKind">The kind of object to check.</param>
	/// <param name="objectId">The ID of the object to check.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The result of the authorization check.</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types - FgaResult and FgaCheckRequest and nested types - are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public async Task<FgaResult?> Check(
		string user,
		string relation,
		string objectKind,
		string objectId,
		CancellationToken cancellationToken = default
	)
	{
		logger.LogInformation("Checking authorization for user '{User}' with relation {Relation} on {ObjectKind}:{ObjectId}",
			user, relation, objectKind, objectId);

		var request = new FgaCheckRequest
		{
			AuthorizationModelId = config.ModelId,
			TupleKey = new()
			{
				User = $"user:{user}",
				Relation = relation,
				Object = $"{objectKind}:{objectId}",
			},
		};
		var response = await http.PostAsJsonAsync(
			$"/stores/{config.StoreId}/check",
			request,
			Json,
			cancellationToken
		);

		return await response.Content.ReadFromJsonAsync<FgaResult>(
			Json, cancellationToken: cancellationToken);
	}

	/// <summary>
	/// Write or delete a relationship tuple between a user and an object.
	/// </summary>
	/// <param name="user">The user the relationship applies to.</param>
	/// <param name="relation">The relationship to write or delete.</param>
	/// <param name="objectKind">The kind of object the relationship applies to.</param>
	/// <param name="objectId">The ID of the object the relationship applies to.</param>
	/// <param name="delete">Whether to delete the tuple instead of writing it.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types - FgaWriteRequest and FgaTupleKeys and nested types - are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public async Task Write(
		string user,
		string relation,
		string objectKind,
		string objectId,
		bool delete = false,
		CancellationToken cancellationToken = default
	)
	{
		logger.LogInformation("{Action} tuple for user '{User}' with relation {Relation} on {ObjectKind}:{ObjectId}",
			delete ? "Deleting" : "Writing", user, relation, objectKind, objectId);

		var tuple = new FgaTuple
		{
			User = $"user:{user}",
			Relation = relation,
			Object = $"{objectKind}:{objectId}",
		};
		var request = new FgaWriteRequest
		{
			AuthorizationModelId = config.ModelId,
			Writes = delete ? null : new FgaTupleKeys { TupleKeys = [tuple] },
			Deletes = delete ? new FgaTupleKeys { TupleKeys = [tuple] } : null,
		};
		var response = await http.PostAsJsonAsync(
			$"/stores/{config.StoreId}/write",
			request,
			Json,
			cancellationToken
		);

		response.EnsureSuccessStatusCode();
	}
}

public sealed record FgaCheckRequest
{
	public required string AuthorizationModelId { get; init; }
	public required FgaTuple TupleKey { get; init; }
}

public sealed record FgaTuple
{
	/// <summary>The user to check authorization for.</summary>
	/// <example>user:alice</example>
	public required string User { get; init; }
	public required string Relation { get; init; }

	/// <summary>The object to check.</summary>
	/// <example>organization:apple</example>
	public required string Object { get; init; }
}

public sealed record FgaResult
{
	/// <summary>Whether the user has the relationship on the object.</summary>
	/// <example>true</example>
	public required bool Allowed { get; init; }
}

public sealed record FgaWriteRequest
{
	public required string AuthorizationModelId { get; init; }
	public FgaTupleKeys? Writes { get; init; }
	public FgaTupleKeys? Deletes { get; init; }
}

public sealed record FgaTupleKeys
{
	public required IReadOnlyList<FgaTuple> TupleKeys { get; init; }
}

[JsonSerializable(typeof(FgaCheckRequest))]
[JsonSerializable(typeof(FgaTuple))]
[JsonSerializable(typeof(FgaResult))]
[JsonSerializable(typeof(FgaWriteRequest))]
[JsonSerializable(typeof(FgaTupleKeys))]
partial class FgaClientJsonSerializerContext : JsonSerializerContext { }