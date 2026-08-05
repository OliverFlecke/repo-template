using System.Net;
using System.Net.Http.Headers;

namespace Api.Test;

public sealed class AuthTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	[Arguments("GET", "/organization")]
	[Arguments("POST", "/organization")]
	[Arguments("PATCH", "/organization/00000000-0000-0000-0000-000000000000")]
	[Arguments("DELETE", "/organization/00000000-0000-0000-0000-000000000000")]
	public async Task Endpoint_WithoutToken_RespondsUnauthorized(string method, string path)
	{
		var client = App.CreateClient();

		// Verify a request without a token is rejected
		{
			using var request = new HttpRequestMessage(new HttpMethod(method), path);
			var response = await client.SendAsync(request);

			await Assert.That(response).HasStatusCode(HttpStatusCode.Unauthorized);
		}

		// Verify an invalid token is rejected
		{
			using var request = new HttpRequestMessage(new HttpMethod(method), path);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
			var response = await client.SendAsync(request);

			await Assert.That(response).HasStatusCode(HttpStatusCode.Unauthorized);
		}
	}
}