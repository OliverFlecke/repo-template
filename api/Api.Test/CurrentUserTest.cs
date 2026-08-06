using System.Security.Claims;

using Api.Auth;

using Microsoft.AspNetCore.Http;

namespace Api.Test;

public sealed class CurrentUserTest
{
	[Test]
	public async Task Id_ReadsSubjectClaim()
	{
		var currentUser = CurrentUserFor(new Claim("sub", "user-123"));

		await Assert.That(currentUser.Id).IsEqualTo("user-123");
	}

	[Test]
	public async Task Email_ReadsEmailClaim()
	{
		var currentUser = CurrentUserFor(new Claim("sub", "user-123"), new Claim("email", "someone@example.com"));

		await Assert.That(currentUser.Email).IsEqualTo("someone@example.com");
	}

	[Test]
	public async Task Email_IsNullWhenClaimMissing()
	{
		var currentUser = CurrentUserFor(new Claim("sub", "user-123"));

		await Assert.That(currentUser.Email).IsNull();
	}

	[Test]
	public async Task Id_ThrowsWhenNoSubjectClaimPresent()
	{
		var currentUser = CurrentUserFor(new Claim("email", "someone@example.com"));

		await Assert.That(() => currentUser.Id).Throws<InvalidOperationException>();
	}

	static ICurrentUser CurrentUserFor(params Claim[] claims)
	{
		var accessor = new HttpContextAccessor
		{
			HttpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
			},
		};

		return new CurrentUser(accessor);
	}
}