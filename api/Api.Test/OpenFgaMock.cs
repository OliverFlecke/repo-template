using System.Text.Json;

using TUnit.Core.Interfaces;

using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Api.Test;

public sealed class OpenFgaMock : IAsyncInitializer, IAsyncDisposable
{
	WireMockServer server = null!;

	public Uri Host => new(server.Url!);

	public Task InitializeAsync()
	{
		server = WireMockServer.Start();

		// Anything not explicitly mocked via MockCheck is treated as denied. Without this,
		// OpenFgaApiClient.Check's ReadFromJsonAsync would throw on WireMock's default
		// (non-JSON) 404 response, turning "not an admin" into a 500 instead of a 403.
		server
			.Given(Request.Create().WithPath(p => p.EndsWith("/check")).UsingPost())
			.AtPriority(1000)
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { allowed = false }));

		// Same reasoning as the /check catch-all above: any unmocked /write call should
		// succeed by default rather than 500, since most tests don't care about the exact
		// tuple written and only assert on it via WasWriteCalled when they do.
		server
			.Given(Request.Create().WithPath(p => p.EndsWith("/write")).UsingPost())
			.AtPriority(1000)
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { }));

		return Task.CompletedTask;
	}

	public void MockCheck(string user, string relation, string objectKind, string objectId, bool allowed)
	{
		server
			.Given(Request.Create()
				.WithPath(p => p.EndsWith("/check"))
				.UsingPost()
				.WithBody(new JsonPartialMatcher(new
				{
					tuple_key = new
					{
						user = $"user:{user}",
						relation,
						@object = $"{objectKind}:{objectId}",
					},
				})))
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { allowed }));
	}

	/// <summary>Polls the recorded requests for a /write call that wrote (or deleted) the given
	/// tuple. Wolverine dispatches the handler that triggers this call asynchronously relative to
	/// the HTTP response that appended the triggering event, so this can't be checked immediately.</summary>
	public async Task<bool> WasWriteCalled(
		string user, string relation, string objectKind, string objectId,
		bool delete = false, TimeSpan? timeout = null)
	{
		var expectedUser = $"user:{user}";
		var expectedObject = $"{objectKind}:{objectId}";
		var key = delete ? "deletes" : "writes";
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

		while (true)
		{
			var matched = server.LogEntries.Any(entry =>
			{
				if (entry.RequestMessage is not { Path: var path, Body: { } body } || !path.EndsWith("/write"))
				{
					return false;
				}

				using var doc = JsonDocument.Parse(body);
				if (!doc.RootElement.TryGetProperty(key, out var tupleKeys) || tupleKeys.ValueKind != JsonValueKind.Object)
				{
					return false;
				}

				return tupleKeys.GetProperty("tuple_keys").EnumerateArray().Any(t =>
					t.GetProperty("user").GetString() == expectedUser &&
					t.GetProperty("relation").GetString() == relation &&
					t.GetProperty("object").GetString() == expectedObject);
			});

			if (matched)
			{
				return true;
			}

			if (DateTime.UtcNow >= deadline)
			{
				return false;
			}

			await Task.Delay(50);
		}
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		server.Stop();
		server.Dispose();
		return ValueTask.CompletedTask;
	}
}