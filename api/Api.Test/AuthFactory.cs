using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

using Bogus;

using Microsoft.IdentityModel.Tokens;

namespace Api.Test;

public static class AuthFactory
{
	// Test-only signing key, used to issue self-signed Bearer tokens instead of relying on a live authority.
	public static readonly SymmetricSecurityKey TestSigningKey =
		new(Encoding.UTF8.GetBytes("integration-test-signing-key-do-not-use-outside-of-tests"));

	public static readonly string Audience = new Faker().Internet.Url();

	public static readonly string Issuer = new Faker().Internet.Url();

	static string CreateToken(string subject, string? email)
	{
		var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, subject) };
		if (email is not null)
		{
			claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
		}

		var token = new JwtSecurityToken(
			audience: Audience,
			issuer: Issuer,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(5),
			signingCredentials: new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256));

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public static HttpClient WithAuthenticatedUser(
		this HttpClient client,
		string subject = "test-user",
		string? email = "test-user@example.com"
	)
	{
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
			"Bearer", CreateToken(subject, email));

		return client;
	}
}