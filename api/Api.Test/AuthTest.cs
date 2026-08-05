using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Api.Org.Endpoint;

namespace Api.Test;

public sealed class AuthTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	[Arguments("POST", "/organization")]
	public async Task Endpoint_WithoutToken_RespondsUnauthorized(string method, string path)
	{
		var client = App.CreateClient();

		{
			using var request = new HttpRequestMessage(new HttpMethod(method), path);
			var response = await client.SendAsync(request);

			await Assert.That(response).HasStatusCode(HttpStatusCode.Unauthorized);
		}

		{
			using var request = new HttpRequestMessage(new HttpMethod(method), path);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
			var response = await client.PostAsJsonAsync("/organization", new CreateOrganizationRequest { Name = "Unauthorized Co" });

			await Assert.That(response).HasStatusCode(HttpStatusCode.Unauthorized);
		}
	}
}