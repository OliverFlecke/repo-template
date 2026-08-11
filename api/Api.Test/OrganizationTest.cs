using System.Net;
using System.Net.Http.Json;

using Api.Common;
using Api.Org.Endpoint;

using Marten;
using Marten.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Test;

public sealed class OrganizationTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	readonly string subject = Guid.NewGuid().ToString();

	[Before(HookType.Test)]
	public Task SetupAdminAccess()
	{
		App.OpenFga.MockCheck(subject, "admin", "system", "core", allowed: true);
		return Task.CompletedTask;
	}

	[Test]
	public async Task Organization_Create_RespondWithOk200()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var body = new CreateOrganizationRequest { Name = "Apple" };

		var response = await client.PostAsJsonAsync("api/v1/admin/organization", body);

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

	[Test]
	public async Task Organization_List_SortsAscendingByNameByDefault()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var first = await client.CreateOrganization($"AAA-{Guid.NewGuid()}");
		var second = await client.CreateOrganization($"ZZZ-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var page = await client.GetOrganizations();

		var relevant = page.Data.Where(o => o.Id == first.Id || o.Id == second.Id).ToList();
		await Assert.That(relevant.Count).IsEqualTo(2);
		await Assert.That(relevant[0].Id).IsEqualTo(first.Id);
		await Assert.That(relevant[1].Id).IsEqualTo(second.Id);
	}

	[Test]
	public async Task Organization_List_SortsDescendingWhenRequested()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var first = await client.CreateOrganization($"AAA-{Guid.NewGuid()}");
		var second = await client.CreateOrganization($"ZZZ-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var page = await client.GetOrganizations("?sortDescending=true");

		var relevant = page.Data.Where(o => o.Id == first.Id || o.Id == second.Id).ToList();
		await Assert.That(relevant.Count).IsEqualTo(2);
		await Assert.That(relevant[0].Id).IsEqualTo(second.Id);
		await Assert.That(relevant[1].Id).IsEqualTo(first.Id);
	}

	[Test]
	public async Task Organization_List_RespectsRequestedPageSize()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		await client.CreateOrganization($"Paging-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var page = await client.GetOrganizations("?page=1&pageSize=1");

		await Assert.That(page.Data.Count).IsEqualTo(1);
		await Assert.That(page.Page).IsEqualTo(1);
		await Assert.That(page.PageSize).IsEqualTo(1);
	}

	[Test]
	public async Task Organization_List_FiltersBySearchTerm()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var match = await client.CreateOrganization($"SearchableApple-{Guid.NewGuid()}");
		var nonMatch = await client.CreateOrganization($"Banana-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var page = await client.GetOrganizations("?search=SearchableApple");

		await Assert.That(page.Data.Any(o => o.Id == match.Id)).IsTrue();
		await Assert.That(page.Data.Any(o => o.Id == nonMatch.Id)).IsFalse();
	}

	[Test]
	public async Task Organization_List_SearchIsCaseInsensitive()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var match = await client.CreateOrganization($"CaseTest-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var page = await client.GetOrganizations("?search=casetest");

		await Assert.That(page.Data.Any(o => o.Id == match.Id)).IsTrue();
	}

	[Test]
	[Arguments("?page=0")]
	[Arguments("?page=-1")]
	public async Task Organization_List_WithInvalidPage_RespondsBadRequest(string query)
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.GetAsync($"api/v1/admin/organization{query}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.BadRequest);
	}

	[Test]
	[Arguments("?pageSize=0")]
	[Arguments("?pageSize=101")]
	public async Task Organization_List_WithInvalidPageSize_RespondsBadRequest(string query)
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.GetAsync($"api/v1/admin/organization{query}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.BadRequest);
	}

	[Test]
	public async Task Organization_Update_RespondsOkAndUpdatesProjection()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var created = await client.CreateOrganization($"Original-{Guid.NewGuid()}");

		var newName = $"Updated-{Guid.NewGuid()}";
		var response = await client.PatchAsJsonAsync($"api/v1/admin/organization/{created.Id}", new UpdateOrganizationRequest { Name = newName });

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		var updated = await Assert.That(await response.Content.ReadFromJsonAsync<Org.Response.Organization>()).IsNotNull();
		await Assert.That(updated.Id).IsEqualTo(created.Id);
		await Assert.That(updated.Name).IsEqualTo(newName);

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(created.Id))
			.WaitsFor(x => x.IsNotNull(), timeout: TimeSpan.FromSeconds(5)).And.IsNotNull();
		await Assert.That(projected.Name).IsEqualTo(newName);
	}

	[Test]
	public async Task Organization_Update_WithUnknownId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PatchAsJsonAsync($"api/v1/admin/organization/{Guid.NewGuid()}", new UpdateOrganizationRequest { Name = "Doesn't matter" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task Organization_Delete_RespondsOkAndRemovesProjection()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var created = await client.CreateOrganization($"ToDelete-{Guid.NewGuid()}");

		var response = await client.DeleteAsync($"api/v1/admin/organization/{created.Id}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(created.Id))
			.WaitsFor(x => x.IsNull(), timeout: TimeSpan.FromSeconds(5));
	}

	[Test]
	public async Task Organization_Delete_WithUnknownId_RespondsOk()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.DeleteAsync($"api/v1/admin/organization/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}

	[Test]
	public async Task Organization_Delete_CalledTwice_BothCallsRespondOk()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var created = await client.CreateOrganization($"DeleteTwice-{Guid.NewGuid()}");

		var firstResponse = await client.DeleteAsync($"api/v1/admin/organization/{created.Id}");
		await Assert.That(firstResponse).HasStatusCode(HttpStatusCode.OK);

		var secondResponse = await client.DeleteAsync($"api/v1/admin/organization/{created.Id}");
		await Assert.That(secondResponse).HasStatusCode(HttpStatusCode.OK);
	}

	[Test]
	public async Task Organization_Delete_RemovesOrganizationFromList()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var created = await client.CreateOrganization($"DeleteFromList-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		await Assert.That(await client.GetOrganizations()).Member(x => x.Data, x => x.Any(o => o.Id == created.Id));

		var deleteResponse = await client.DeleteAsync($"api/v1/admin/organization/{created.Id}");
		await Assert.That(deleteResponse).HasStatusCode(HttpStatusCode.OK);

		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var afterDelete = await client.GetOrganizations();
		await Assert.That(afterDelete.Data.Any(o => o.Id == created.Id)).IsFalse();
	}

}

public static class ClientOrganizationExtensions
{
	public static async Task<Org.Response.Organization> CreateOrganization(this HttpClient client, string name)
	{
		var response = await client.PostAsJsonAsync("api/v1/admin/organization", new CreateOrganizationRequest { Name = name });
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<Org.Response.Organization>())!;
	}

	public static async Task<PagedResponse<Org.Response.Organization>> GetOrganizations(
		this HttpClient client,
		string? query = default)
	{
		var response = await client.GetAsync($"api/v1/admin/organization{query}");
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<PagedResponse<Org.Response.Organization>>())!;
	}
}