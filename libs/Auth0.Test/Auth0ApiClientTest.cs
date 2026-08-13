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

		await client.UpdateEmail("auth0|abc123", "jane@example.com");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/users/auth0|abc123").RequestMessage!;
		await Assert.That(request.Body).Contains("\"email\":\"jane@example.com\"");
		await Assert.That(request.Body).Contains("\"connection\":\"Username-Password-Authentication\"");
	}

	[Test]
	public async Task UpdateEmail_SetsVerifyEmailSoAuth0SendsItItself()
	{
		mock.MockPatchUser("auth0|abc123");

		await client.UpdateEmail("auth0|abc123", "jane@example.com");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/users/auth0|abc123").RequestMessage!;
		await Assert.That(request.Body).Contains("\"verify_email\":true");
	}

	[Test]
	public async Task UpdatePassword_SendsPatchWithPasswordAndConnection()
	{
		mock.MockPatchUser("auth0|abc123");

		await client.UpdatePassword("auth0|abc123", "s3cr3t-p@ssword");

		var request = mock.RequestLog.Single(e => e.RequestMessage?.Path == "/api/v2/users/auth0|abc123").RequestMessage!;
		await Assert.That(request.Body).Contains("\"password\":\"s3cr3t-p@ssword\"");
		await Assert.That(request.Body).Contains("\"connection\":\"Username-Password-Authentication\"");
	}

	[Test]
	public async Task GetUser_ReturnsProfile()
	{
		mock.MockGetUser("auth0|abc123", "Jane Doe", "jane@example.com");

		var profile = await client.GetUser("auth0|abc123");

		await Assert.That(profile!.Name).IsEqualTo("Jane Doe");
		await Assert.That(profile.Email).IsEqualTo("jane@example.com");
	}

	[Test]
	public async Task GetUser_WhenAuth0ReturnsNotFound_ReturnsNull()
	{
		mock.MockError("/api/v2/users/auth0|abc123", 404, "not found");

		var profile = await client.GetUser("auth0|abc123");

		await Assert.That(profile).IsNull();
	}

	[Test]
	public async Task GetUsers_OmitsUsersAuth0DoesNotKnowAbout()
	{
		mock.MockGetUser("auth0|abc123", "Jane Doe");
		mock.MockError("/api/v2/users/auth0|missing", 404, "not found");

		var profiles = await client.GetUsers(["auth0|abc123", "auth0|missing"]);

		await Assert.That(profiles.ContainsKey("auth0|abc123")).IsTrue();
		await Assert.That(profiles.ContainsKey("auth0|missing")).IsFalse();
	}

	[Test]
	public async Task GetUsers_OmitsRatherThanThrowsWhenAuth0IsUnreachableOrErrors()
	{
		mock.MockGetUser("auth0|abc123", "Jane Doe");
		mock.MockError("/api/v2/users/auth0|broken", 500, "internal error");

		var profiles = await client.GetUsers(["auth0|abc123", "auth0|broken"]);

		await Assert.That(profiles.ContainsKey("auth0|abc123")).IsTrue();
		await Assert.That(profiles.ContainsKey("auth0|broken")).IsFalse();
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

	[Test]
	public async Task WhenAuth0ReturnsNonJsonError_ThrowsAuth0ApiExceptionInsteadOfJsonException()
	{
		mock.MockNonJsonError("/api/v2/users/auth0|abc123", 502, "<html>Bad Gateway</html>");

		await Assert.That(async () => await client.UpdateEmail("auth0|abc123", "jane@example.com"))
			.Throws<Auth0ApiException>();
	}
}