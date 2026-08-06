using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Api.OpenFGA;

public static class OpenFgaSetup
{
	public static void SetupOpenFga(this IHostApplicationBuilder builder)
	{
		builder.Services.AddOptions<OpenFgaConfig>()
			.BindConfiguration(OpenFgaConfig.Section)
			.ValidateOnStart();
		builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpenFgaConfig>>().Value);

		builder.Services.AddHttpClient<OpenFgaApiClient>((sp, http) =>
		{
			var config = sp.GetRequiredService<IOptions<OpenFgaConfig>>();
			http.BaseAddress = config.Value.Host;
		});

		builder.Services.AddScoped<IAuthorizationHandler, OpenFgaAuthorizationHandler>();
	}
}