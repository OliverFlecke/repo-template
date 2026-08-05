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

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		server.Stop();
		server.Dispose();
		return ValueTask.CompletedTask;
	}
}