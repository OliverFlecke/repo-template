using System.Net;

namespace Api.Test;

public sealed class HealthCheckTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory WebApplicationFactory { get; init; }

	[Test]
	public async Task HealthCheck_RespondWithOk200()
	{
		var client = WebApplicationFactory.CreateClient();

		var response = await client.GetAsync("/healthz");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}
}