using System.Text.Json.Nodes;

using TUnit.Core.Interfaces;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Acra.Test;

/// <summary>Reusable WireMock stub for the ACRA datastore_search endpoint. Add a
/// ProjectReference to this test project to reuse it when testing code that calls
/// AcraApiClient.</summary>
public sealed class AcraMock : IAsyncInitializer, IAsyncDisposable
{
	WireMockServer server = null!;

	public Uri Host => new(server.Url!);

	public IReadOnlyList<string> RecordedQueries =>
		server.LogEntries.Select(e => e.RequestMessage?.RawQuery ?? "").ToList();

	public Task InitializeAsync()
	{
		server = WireMockServer.Start();
		return Task.CompletedTask;
	}

	public void MockSearchByUen(string uen, AcraEntity? entity)
	{
		var records = entity is null ? [] : new[] { ToRecord(entity) };

		server
			.Given(Request.Create()
				.WithPath("/api/action/datastore_search")
				.UsingGet()
				.WithParam("filters", new JsonObject { ["uen"] = uen }.ToJsonString()))
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
			{
				success = true,
				result = new { records, total = records.Length },
			}));
	}

	public void MockSearchByName(
		string name,
		IReadOnlyList<AcraEntity> entities,
		int? total = null,
		int limit = 100,
		int offset = 0
	)
	{
		server
			.Given(Request.Create()
				.WithPath("/api/action/datastore_search")
				.UsingGet()
				.WithParam("q", new JsonObject { ["entity_name"] = name }.ToJsonString())
				.WithParam("limit", limit.ToString())
				.WithParam("offset", offset.ToString()))
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
			{
				success = true,
				result = new
				{
					records = entities.Select(ToRecord),
					total = total ?? entities.Count,
				},
			}));
	}

	/// <summary>Makes every datastore_search call on this server return a CKAN-style error envelope.</summary>
	public void MockError(string message)
	{
		server
			.Given(Request.Create().WithPath("/api/action/datastore_search").UsingGet())
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
			{
				success = false,
				error = new { message },
			}));
	}

	/// <summary>Makes every datastore_search call on this server return 503, to exercise the
	/// retry behaviour wired up via AddStandardResilienceHandler in AcraSetup.</summary>
	public void MockAlwaysUnavailable()
	{
		server
			.Given(Request.Create().WithPath("/api/action/datastore_search").UsingGet())
			.RespondWith(Response.Create().WithStatusCode(503));
	}

	static object ToRecord(AcraEntity entity) => new
	{
		uen = entity.Uen,
		entity_name = entity.EntityName,
		issuance_agency_desc = entity.IssuanceAgencyDesc,
		uen_status_desc = entity.UenStatusDesc,
		entity_type_desc = entity.EntityTypeDesc,
		uen_issue_date = entity.UenIssueDate,
		reg_street_name = entity.RegStreetName,
		reg_postal_code = entity.RegPostalCode,
	};

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		server.Stop();
		server.Dispose();
		return ValueTask.CompletedTask;
	}
}
