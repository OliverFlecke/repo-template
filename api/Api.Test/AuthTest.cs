using System.Net;
using System.Net.Http.Headers;

namespace Api.Test;

public sealed class AuthTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	[Arguments("GET", "v1/admin/organization")]
	[Arguments("POST", "v1/admin/organization")]
	[Arguments("PATCH", "v1/admin/organization/00000000-0000-0000-0000-000000000000")]
	[Arguments("DELETE", "v1/admin/organization/00000000-0000-0000-0000-000000000000")]
	[Arguments("GET", "v1/organization")]
	[Arguments("POST", "v1/organization")]
	[Arguments("POST", "v1/organization/00000000-0000-0000-0000-000000000000/member")]
	[Arguments("DELETE", "v1/organization/00000000-0000-0000-0000-000000000000/member/some-user")]
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

	[Test]
	[Arguments("GET", "v1/admin/organization")]
	[Arguments("POST", "v1/admin/organization")]
	[Arguments("PATCH", "v1/admin/organization/00000000-0000-0000-0000-000000000000")]
	[Arguments("DELETE", "v1/admin/organization/00000000-0000-0000-0000-000000000000")]
	public async Task Endpoint_WhenUserIsNotAdmin_RespondsForbidden(string method, string path)
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject: "not-an-admin");

		using var request = new HttpRequestMessage(new HttpMethod(method), path);
		var response = await client.SendAsync(request);

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	[Arguments("POST", "v1/organization/00000000-0000-0000-0000-000000000000/member")]
	[Arguments("DELETE", "v1/organization/00000000-0000-0000-0000-000000000000/member/some-user")]
	public async Task Endpoint_WhenUserCannotAddToOrganization_RespondsForbidden(string method, string path)
	{
		var subject = Guid.NewGuid().ToString();
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		App.OpenFga.MockCheck(subject, "can_add", "organization", "00000000-0000-0000-0000-000000000000", allowed: false);

		using var request = new HttpRequestMessage(new HttpMethod(method), path);
		var response = await client.SendAsync(request);

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}
}