using System.Net;
using System.Net.Http.Json;

namespace Api.Test;

public sealed class AccountTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	public async Task UpdatePassword_WhenUserIsNotOnDatabaseConnection_RespondsForbidden()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject: "google-oauth2|123");

		var response = await client.PatchAsJsonAsync("api/v1/account/password", new { password = "correct horse battery staple" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task UpdatePassword_WhenUserIsOnDatabaseConnection_UpdatesPassword()
	{
		var subject = $"auth0|{Guid.NewGuid()}";
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		App.Auth0.MockPatchUser(subject);

		var response = await client.PatchAsJsonAsync("api/v1/account/password", new { password = "correct horse battery staple" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}
}
