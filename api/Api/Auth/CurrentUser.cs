using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Auth;

/// <summary>Exposes identity information for the user attached to the current request, as extracted from the incoming Bearer token.</summary>
public interface ICurrentUser
{
	/// <summary>The `sub` claim identifying the authenticated user.</summary>
	string Id { get; }

	/// <summary>The `email` claim, if present.</summary>
	string? Email { get; }

	/// <summary>The `name` claim, if present.</summary>
	string? Name { get; }

	/// <summary>All claims present on the current token.</summary>
	IReadOnlyList<Claim> Claims { get; }

	/// <summary>Returns the value of the first claim of the given type, or null if none is present.</summary>
	string? FindClaim(string type);
}

sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	ClaimsPrincipal User =>
		httpContextAccessor.HttpContext?.User
		?? throw new InvalidOperationException($"{nameof(ICurrentUser)} was used outside of an HTTP request.");

	public string Id =>
		FindClaim(JwtRegisteredClaimNames.Sub)
		?? FindClaim(ClaimTypes.NameIdentifier)
		?? throw new InvalidOperationException("The current user's token has no subject claim.");

	public string? Email => FindClaim(JwtRegisteredClaimNames.Email) ?? FindClaim(ClaimTypes.Email);

	public string? Name => FindClaim(JwtRegisteredClaimNames.Name) ?? FindClaim(ClaimTypes.Name);

	public IReadOnlyList<Claim> Claims => [.. User.Claims];

	public string? FindClaim(string type) => User.FindFirst(type)?.Value;
}