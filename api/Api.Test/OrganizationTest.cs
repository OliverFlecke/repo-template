using System.Net;
using System.Net.Http.Json;

using Api.Org.Endpoint;

using Marten;
using Marten.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Test;

public sealed class OrganizationTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	[Test]
	public async Task Organization_Create_RespondWithOk200()
	{
		var client = App.CreateClient().WithAuthenticatedUser();

		var body = new CreateOrganizationRequest { Name = "Apple" };

		var response = await client.PostAsJsonAsync("/organization", body);

		// Assert
		await Assert.That(response).HasStatusCode(HttpStatusCode.Created);

		var org = await Assert.That(await response.Content.ReadFromJsonAsync<Org.Response.Organization>()).IsNotNull();
		await Assert.That(response).HasHeader("Location");
		await Assert.That(response.Headers.Location?.ToString()).EndsWith($"/organization/{org.Id}");

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var stream = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id))
			.WaitsFor(x => x.IsNotNull(), timeout: TimeSpan.FromSeconds(5)).And.IsNotNull();

		await Assert.That(stream.Name).IsEqualTo(org.Name);
		await Assert.That(stream.Id).IsEqualTo(org.Id);
	}
}