using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Auth0;

public static class Auth0Setup
{
	public static void SetupAuth0(this IHostApplicationBuilder builder)
	{
		builder.Services.AddOptions<Auth0Config>()
			.BindConfiguration(Auth0Config.Section)
			.ValidateOnStart();
		builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Auth0Config>>().Value);

		builder.Services.AddHttpClient<Auth0ApiClient>((sp, http) =>
			{
				var config = sp.GetRequiredService<IOptions<Auth0Config>>().Value;
				http.BaseAddress = config.Host;
			})
			.AddStandardResilienceHandler();
	}
}