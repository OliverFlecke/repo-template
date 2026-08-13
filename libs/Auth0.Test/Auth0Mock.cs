using TUnit.Core.Interfaces;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Auth0.Test;

/// <summary>
/// Reusable WireMock stub for the Auth0 endpoints Auth0ApiClient calls. Add a ProjectReference
/// to this test project to reuse it when testing code that calls Auth0ApiClient.
/// </summary>
public sealed class Auth0Mock : IAsyncInitializer, IAsyncDisposable
{
	WireMockServer server = null!;

	public Uri Host => new(server.Url!);

	public IReadOnlyList<WireMock.Logging.ILogEntry> RequestLog => server.LogEntries.ToList();

	public int TokenRequestCount =>
		RequestLog.Count(e => e.RequestMessage?.Path == "/oauth/token");

	public Task InitializeAsync()
	{
		server = WireMockServer.Start();
		MockToken();
		return Task.CompletedTask;
	}

	public void MockToken(string accessToken = "management-token", int expiresIn = 86400)
	{
		server
			.Given(Request.Create().WithPath("/oauth/token").UsingPost())
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
			{
				access_token = accessToken,
				expires_in = expiresIn,
				token_type = "Bearer",
			}));
	}

	public void MockPatchUser(string userId)
	{
		server
			.Given(Request.Create().WithPath($"/api/v2/users/{userId}").UsingPatch())
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { user_id = userId }));
	}

	public void MockGetUser(string userId, string name, string? email = null)
	{
		server
			.Given(Request.Create().WithPath($"/api/v2/users/{userId}").UsingGet())
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { user_id = userId, name, email }));
	}

	public void MockVerificationEmail()
	{
		server
			.Given(Request.Create().WithPath("/api/v2/jobs/verification-email").UsingPost())
			.RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { status = "pending" }));
	}

	public void MockError(string path, int statusCode, string message)
	{
		server
			.Given(Request.Create().WithPath(path))
			.RespondWith(Response.Create().WithStatusCode(statusCode).WithBodyAsJson(new { message }));
	}

	/// <summary>
	/// Simulates a failure that doesn't come from Auth0 itself - a gateway/proxy returning a
	/// plain-text or HTML error page instead of Auth0's usual JSON error body.
	/// </summary>
	public void MockNonJsonError(string path, int statusCode, string body)
	{
		server
			.Given(Request.Create().WithPath(path))
			.RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		server.Stop();
		server.Dispose();
		return ValueTask.CompletedTask;
	}
}