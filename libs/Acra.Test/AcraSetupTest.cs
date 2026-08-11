using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Acra.Test;

public sealed class AcraSetupTest
{
	[Test]
	public async Task SetupAcra_RetriesTransientFailures()
	{
		await using var mock = new AcraMock();
		await mock.InitializeAsync();
		mock.MockAlwaysUnavailable();

		var builder = Host.CreateApplicationBuilder();
		builder.Logging.ClearProviders();
		builder.Configuration["Acra:Host"] = mock.Host.ToString();
		builder.SetupAcra();
		using var host = builder.Build();
		var client = host.Services.GetRequiredService<AcraApiClient>();

		await Assert.That(async () => await client.SearchByUen("ANY")).Throws<HttpRequestException>();

		// Standard resilience handler retries transient failures (5xx, 408, 429, network errors)
		// 3 times by default, so the initial attempt plus 3 retries is 4 total requests.
		await Assert.That(mock.RecordedQueries.Count).IsEqualTo(4);
	}
}
