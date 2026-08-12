using Microsoft.Extensions.Logging.Abstractions;

namespace Auth0.Test;

public sealed class Auth0ApiClientTest
{
	Auth0Mock mock = null!;
	Auth0ApiClient client = null!;

	[Before(HookType.Test)]
	public async Task Setup()
	{
		mock = new Auth0Mock();
		await mock.InitializeAsync();

		var config = new Auth0Config
		{
			Host = mock.Host,
			ClientId = "client-id",
			ClientSecret = "client-secret",
		};
		client = new Auth0ApiClient(NullLogger<Auth0ApiClient>.Instance, config, new HttpClient { BaseAddress = mock.Host });
	}

	[After(HookType.Test)]
	public async Task Teardown() => await mock.DisposeAsync();

	[Test]
	public async Task UpdateName_SendsPatchWithName()
	{
		mock.MockPatchUser("auth0|abc123");

		await client.UpdateName("auth0|abc123", "Jane Doe");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/users/auth0|abc123").RequestMessage!;
		await Assert.That(request.Body).Contains("\"name\":\"Jane Doe\"");
		await Assert.That(request.Headers!["Authorization"].ToString()).Contains("management-token");
	}

	[Test]
	public async Task UpdateEmail_SendsPatchWithEmailAndConnection()
	{
		mock.MockPatchUser("auth0|abc123");
		mock.MockVerificationEmail();

		await client.UpdateEmail("auth0|abc123", "jane@example.com");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/users/auth0|abc123").RequestMessage!;
		await Assert.That(request.Body).Contains("\"email\":\"jane@example.com\"");
		await Assert.That(request.Body).Contains("\"connection\":\"Username-Password-Authentication\"");
	}

	[Test]
	public async Task UpdateEmail_AlsoSendsVerificationEmail()
	{
		mock.MockPatchUser("auth0|abc123");
		mock.MockVerificationEmail();

		await client.UpdateEmail("auth0|abc123", "jane@example.com");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/jobs/verification-email").RequestMessage!;
		await Assert.That(request.Body).Contains("\"user_id\":\"auth0|abc123\"");
	}

	[Test]
	public async Task SendVerificationEmail_PostsJobWithUserId()
	{
		mock.MockVerificationEmail();

		await client.SendVerificationEmail("auth0|abc123");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/jobs/verification-email").RequestMessage!;
		await Assert.That(request.Body).Contains("\"user_id\":\"auth0|abc123\"");
	}

	[Test]
	public async Task ManagementToken_IsCachedAcrossCalls()
	{
		mock.MockPatchUser("auth0|abc123");

		await client.UpdateName("auth0|abc123", "First");
		await client.UpdateName("auth0|abc123", "Second");

		await Assert.That(mock.TokenRequestCount).IsEqualTo(1);
	}

	[Test]
	public async Task WhenAuth0ReturnsError_ThrowsAuth0ApiException()
	{
		mock.MockError("/api/v2/users/auth0|abc123", 400, "email format is invalid");

		await Assert.That(async () => await client.UpdateEmail("auth0|abc123", "not-an-email"))
			.Throws<Auth0ApiException>()
			.WithMessage("email format is invalid");
	}
}