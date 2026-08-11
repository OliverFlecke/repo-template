using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Acra;

public static class AcraSetup
{
	public static void SetupAcra(this IHostApplicationBuilder builder)
	{
		builder.Services.AddOptions<AcraConfig>().BindConfiguration(AcraConfig.Section);
		builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AcraConfig>>().Value);

		builder.Services.AddHttpClient<AcraApiClient>((sp, http) =>
			{
				var config = sp.GetRequiredService<IOptions<AcraConfig>>().Value;
				http.BaseAddress = config.Host;
				if (!string.IsNullOrEmpty(config.ApiKey))
				{
					http.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
				}
			})
			// Retries transient failures (network errors, timeouts, 5xx, 408, 429) with
			// exponential backoff + jitter, plus a circuit breaker and per-attempt timeout.
			.AddStandardResilienceHandler();
	}
}
