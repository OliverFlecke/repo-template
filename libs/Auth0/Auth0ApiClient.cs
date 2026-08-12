using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace Auth0;

/// <summary>Typed client for the slice of Auth0's Management API needed to let a signed-in user
/// update their own profile. Authenticates itself against the Management API via the
/// client_credentials grant, caching the resulting token until shortly before it expires.</summary>
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

	/// <summary>Updates a user's email address. Auth0 resets email_verified to false as a side
	/// effect of this call, so this also triggers a fresh verification email.</summary>
	public async Task UpdateEmail(string userId, string email, CancellationToken cancellationToken = default)
	{
		await PatchUser(userId, new UpdateUserRequest { Email = email, Connection = config.Connection }, cancellationToken);
		await SendVerificationEmail(userId, cancellationToken);
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

		var error = JsonSerializer.Deserialize<Auth0ErrorResponse>(body, Json);
		throw new Auth0ApiException(error?.Message ?? $"Auth0 API returned {(int)response.StatusCode} {response.ReasonPhrase}");
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
	public string? Connection { get; init; }
}

sealed record VerificationEmailRequest
{
	public required string UserId { get; init; }
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
partial class Auth0JsonSerializerContext : JsonSerializerContext { }