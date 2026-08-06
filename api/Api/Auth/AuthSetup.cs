using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Auth;

public static class AuthSetup
{
	public static void SetupAuthentication(this WebApplicationBuilder builder)
	{
		builder.Services
			.AddOptions<AuthConfig>()
			.BindConfiguration(AuthConfig.SectionKey)
			.ValidateOnStart();

		builder.Services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer();
		builder.Services
			.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
			.Configure<IOptions<AuthConfig>>((jwtOptions, authConfig) =>
			{
				jwtOptions.MapInboundClaims = false;
				jwtOptions.Authority = authConfig.Value.Authority;
				jwtOptions.Audience = authConfig.Value.Audience;
				jwtOptions.RequireHttpsMetadata = authConfig.Value.RequireHttpsMetadata;
			});

		builder.Services.AddAuthorizationBuilder()
			.SetFallbackPolicy(new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build());

		builder.Services.AddHttpContextAccessor();
		builder.Services.AddScoped<ICurrentUser, CurrentUser>();
	}
}