using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace Auth0;

/// <summary>
/// Typed client for the slice of Auth0's Management API needed to let a signed-in user
/// update their own profile. Authenticates itself against the Management API via the
/// client_credentials grant, caching the resulting token until shortly before it expires.
/// </summary>
public sealed class Auth0ApiClient(
	ILogger<Auth0ApiClient> logger,
	Auth0Config config,
	HttpClient http
)
{
	static readonly JsonSerializerOptions Json = new();

	static Auth0ApiClient()
	{
		Json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
		Json.RespectNullableAnnotations = true;
		Json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
		Json.TypeInfoResolverChain.Insert(0, Auth0JsonSerializerContext.Default);
	}

	// ponytail: single cached token behind a lock, no per-tenant/per-scope keying —
	// this client only ever asks for one audience, so a keyed cache would be dead weight.
	readonly SemaphoreSlim tokenLock = new(1, 1);
	ManagementToken? cachedToken;

	/// <summary>Updates a user's display name.</summary>
	public Task UpdateName(string userId, string name, CancellationToken cancellationToken = default) =>
		PatchUser(userId, new UpdateUserRequest { Name = name, Connection = config.Connection }, cancellationToken);

	/// <summary>
	/// Updates a user's email address. Setting verify_email makes Auth0 reset email_verified to
	/// false and send a fresh verification email itself, as part of this same call.
	/// </summary>
	public Task UpdateEmail(string userId, string email, CancellationToken cancellationToken = default) =>
		PatchUser(userId, new UpdateUserRequest { Email = email, Connection = config.Connection, VerifyEmail = true }, cancellationToken);

	/// <summary>
	/// Updates a user's password. Only valid for users on the database connection (a `sub`
	/// starting with `auth0|`) - social/enterprise connections don't have an Auth0-managed
	/// password to change.
	/// </summary>
	public Task UpdatePassword(string userId, string password, CancellationToken cancellationToken = default) =>
		PatchUser(userId, new UpdateUserRequest { Password = password, Connection = config.Connection }, cancellationToken);

	/// <summary>Fetches a user's profile (name, email), or null if Auth0 has no such user.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public async Task<UserProfile?> GetUser(string userId, CancellationToken cancellationToken = default)
	{
		var token = await GetManagementToken(cancellationToken);
		using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/users/{Uri.EscapeDataString(userId)}");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await http.SendAsync(request, cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
		await EnsureSuccess(response, cancellationToken);

		return await response.Content.ReadFromJsonAsync<UserProfile>(Json, cancellationToken);
	}

	/// <summary>Fetches profiles for a set of users, keyed by user id. This is a best-effort
	/// enrichment lookup: users Auth0 doesn't know about (e.g. deleted since) and users a failed
	/// or unreachable Auth0 request couldn't be fetched for are both omitted rather than failing
	/// the whole batch - callers are expected to fall back to something else (e.g. the raw user
	/// id) for any user missing from the result.</summary>
	public async Task<IReadOnlyDictionary<string, UserProfile>> GetUsers(
		IEnumerable<string> userIds,
		CancellationToken cancellationToken = default)
	{
		var profiles = await Task.WhenAll(userIds.Select(async userId =>
		{
			try
			{
				return (userId, profile: await GetUser(userId, cancellationToken));
			}
			catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
			{
				logger.LogWarning(ex, "Failed to fetch Auth0 profile for user {UserId}", userId);
				return (userId, profile: null);
			}
		}));
		return profiles.Where(p => p.profile is not null).ToDictionary(p => p.userId, p => p.profile!);
	}

	/// <summary>Triggers a fresh verification email for the given user.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	public async Task SendVerificationEmail(string userId, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Requesting a verification email for user {UserId}", userId);

		var token = await GetManagementToken(cancellationToken);
		using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/jobs/verification-email")
		{
			Content = JsonContent.Create(new VerificationEmailRequest { UserId = userId }, options: Json),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await http.SendAsync(request, cancellationToken);
		await EnsureSuccess(response, cancellationToken);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	async Task PatchUser(string userId, UpdateUserRequest body, CancellationToken cancellationToken)
	{
		logger.LogInformation("Updating Auth0 user {UserId}", userId);

		var token = await GetManagementToken(cancellationToken);
		using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v2/users/{Uri.EscapeDataString(userId)}")
		{
			Content = JsonContent.Create(body, options: Json),
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await http.SendAsync(request, cancellationToken);
		await EnsureSuccess(response, cancellationToken);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	async Task<string> GetManagementToken(CancellationToken cancellationToken)
	{
		if (cachedToken is { } token && token.ExpiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30))
		{
			return token.AccessToken;
		}

		await tokenLock.WaitAsync(cancellationToken);
		try
		{
			if (cachedToken is { } fresh && fresh.ExpiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30))
			{
				return fresh.AccessToken;
			}

			logger.LogInformation("Fetching a new Auth0 Management API token");

			var response = await http.PostAsJsonAsync("/oauth/token", new TokenRequest
			{
				ClientId = config.ClientId,
				ClientSecret = config.ClientSecret,
				Audience = $"{config.Host}api/v2/",
			}, Json, cancellationToken);

			await EnsureSuccess(response, cancellationToken);

			var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, cancellationToken)
				?? throw new Auth0ApiException("Auth0 token endpoint returned an empty response.");

			cachedToken = new ManagementToken(body.AccessToken, DateTimeOffset.UtcNow.AddSeconds(body.ExpiresIn));
			return cachedToken.AccessToken;
		}
		finally
		{
			tokenLock.Release();
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "All types used are included in the generated code for JsonSerializer.")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.IsSuccessStatusCode)
		{
			return;
		}

		var body = await response.Content.ReadAsStringAsync(cancellationToken);
		logger.LogWarning("Failed request body from Auth0: {Body}", body);

		// Auth0 itself always returns a JSON error body, but an intermediary (a gateway 502
		// during an outage, a proxy timeout page) might not - fall back to the status code
		// instead of letting a JsonException from a non-JSON body mask the real failure.
		string? message;
		try
		{
			message = JsonSerializer.Deserialize<Auth0ErrorResponse>(body, Json)?.Message;
		}
		catch (JsonException)
		{
			message = null;
		}

		throw new Auth0ApiException(message ?? $"Auth0 API returned {(int)response.StatusCode} {response.ReasonPhrase}");
	}
}

sealed record ManagementToken(string AccessToken, DateTimeOffset ExpiresAtUtc);

public sealed class Auth0ApiException(string message) : Exception(message);

sealed record TokenRequest
{
	public required string ClientId { get; init; }
	public required string ClientSecret { get; init; }
	public required string Audience { get; init; }
	public string GrantType { get; init; } = "client_credentials";
}

sealed record TokenResponse
{
	public required string AccessToken { get; init; }
	public required int ExpiresIn { get; init; }
}

sealed record UpdateUserRequest
{
	public string? Name { get; init; }
	public string? Email { get; init; }
	public string? Password { get; init; }
	public string? Connection { get; init; }
	public bool? VerifyEmail { get; init; }
}

sealed record VerificationEmailRequest
{
	public required string UserId { get; init; }
}

public sealed record UserProfile
{
	public required string UserId { get; init; }
	public string? Name { get; init; }
	public string? Email { get; init; }
}

sealed record Auth0ErrorResponse
{
	public string? Message { get; init; }
}

[JsonSerializable(typeof(TokenRequest))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(VerificationEmailRequest))]
[JsonSerializable(typeof(Auth0ErrorResponse))]
[JsonSerializable(typeof(UserProfile))]
partial class Auth0JsonSerializerContext : JsonSerializerContext { }