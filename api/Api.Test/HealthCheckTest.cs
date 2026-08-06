using System.Net;

namespace Api.Test;

public sealed class HealthCheckTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	public async Task HealthCheck_RespondWithOk200()
	{
		var client = App.CreateClient();

		var response = await client.GetAsync("/healthz");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}
}