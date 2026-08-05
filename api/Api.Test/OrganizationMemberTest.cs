using System.Net;
using System.Net.Http.Json;

using Api.Org.Endpoint;
using Api.Org.Model;
using Api.Org.Response;

using Marten;
using Marten.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Test;

public sealed class OrganizationMemberTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	readonly string subject = Guid.NewGuid().ToString();

	[Test]
	public async Task CreateOrganization_AsAnyAuthenticatedUser_RespondsCreated()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PostAsJsonAsync("v1/organization", new CreateOrganizationRequest { Name = "Acme" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Created);
	}

	[Test]
	public async Task CreateOrganization_AddsCreatorAsAdminMemberAndGrantsOpenFgaTuple()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PostAsJsonAsync("v1/organization", new CreateOrganizationRequest { Name = "Acme" });
		var org = (await response.Content.ReadFromJsonAsync<Org.Response.Organization>())!;

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id))
			.WaitsFor(x => x.IsNotNull(), timeout: TimeSpan.FromSeconds(5)).And.IsNotNull();

		await Assert.That(projected.Members[subject]).IsEqualTo(OrganizationRole.Admin);

		var wroteAdminTuple = await App.OpenFga.WasWriteCalled(subject, "admin", "organization", org.Id.ToString());
		await Assert.That(wroteAdminTuple).IsTrue();
	}

	[Test]
	public async Task GetMyOrganizations_ReturnsOnlyOrganizationsCallerBelongsToWithRole()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var otherClient = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());

		var mine = await client.CreateMyOrganization($"Mine-{Guid.NewGuid()}");
		var notMine = await otherClient.CreateMyOrganization($"NotMine-{Guid.NewGuid()}");
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var memberships = await client.GetMyOrganizations();

		await Assert.That(memberships.Any(m => m.Id == mine.Id && m.Role == OrganizationRole.Admin)).IsTrue();
		await Assert.That(memberships.Any(m => m.Id == notMine.Id)).IsFalse();
	}

	[Test]
	public async Task AddMember_WhenCallerCanAdd_AddsMemberAndGrantsOpenFgaTuple()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"AddMember-{Guid.NewGuid()}");
		var newMember = Guid.NewGuid().ToString();
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);

		var response = await client.PostAsJsonAsync(
			$"v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = newMember, Role = OrganizationRole.Member });

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id))
			.WaitsFor(x => x.IsNotNull(), timeout: TimeSpan.FromSeconds(5)).And.IsNotNull();

		await Assert.That(projected.Members[newMember]).IsEqualTo(OrganizationRole.Member);

		var wroteMemberTuple = await App.OpenFga.WasWriteCalled(newMember, "member", "organization", org.Id.ToString());
		await Assert.That(wroteMemberTuple).IsTrue();
	}

	[Test]
	public async Task AddMember_WhenCallerCannotAdd_RespondsForbidden()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"AddMemberForbidden-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: false);

		var response = await client.PostAsJsonAsync(
			$"v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = Guid.NewGuid().ToString(), Role = OrganizationRole.Member });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task AddMember_WithUnknownOrganizationId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var unknownId = Guid.NewGuid();
		App.OpenFga.MockCheck(subject, "can_add", "organization", unknownId.ToString(), allowed: true);

		var response = await client.PostAsJsonAsync(
			$"v1/organization/{unknownId}/member",
			new AddMemberRequest { UserId = Guid.NewGuid().ToString(), Role = OrganizationRole.Member });

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task RemoveMember_WhenCallerCanAdd_RemovesMemberAndDeletesOpenFgaTuple()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"RemoveMember-{Guid.NewGuid()}");
		var member = Guid.NewGuid().ToString();
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		await client.PostAsJsonAsync(
			$"v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = member, Role = OrganizationRole.Member });

		using (var scope = App.Services.CreateScope())
		{
			var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
			var withMember = await WaitForProjection(session, org.Id, o => o?.Members.ContainsKey(member) == true);
			await Assert.That(withMember).IsNotNull();
		}

		var response = await client.DeleteAsync($"v1/organization/{org.Id}/member/{member}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);

		using (var scope = App.Services.CreateScope())
		{
			var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
			var withoutMember = await WaitForProjection(session, org.Id, o => o is not null && !o.Members.ContainsKey(member));
			await Assert.That(withoutMember).IsNotNull();
		}

		var deletedMemberTuple = await App.OpenFga.WasWriteCalled(member, "member", "organization", org.Id.ToString(), delete: true);
		await Assert.That(deletedMemberTuple).IsTrue();
	}

	/// <summary>Polls the async Marten projection until it satisfies the given condition. Used
	/// instead of the simpler `Assert.That(...).WaitsFor(x => x.IsNotNull(), ...)` pattern (see
	/// OrganizationTest.cs) when the condition depends on aggregate state beyond mere existence,
	/// since the aggregate can already exist from an earlier event while a later one is still
	/// catching up.</summary>
	static async Task<Org.Model.Organization?> WaitForProjection(
		IDocumentSession session, Guid id, Func<Org.Model.Organization?, bool> condition, TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
		while (true)
		{
			var org = await session.Events.FetchLatest<Org.Model.Organization>(id);
			if (condition(org) || DateTime.UtcNow >= deadline)
			{
				return org;
			}

			await Task.Delay(50);
		}
	}

	[Test]
	public async Task RemoveMember_WithUnknownOrganizationId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var unknownId = Guid.NewGuid();
		App.OpenFga.MockCheck(subject, "can_add", "organization", unknownId.ToString(), allowed: true);

		var response = await client.DeleteAsync($"v1/organization/{unknownId}/member/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task RemoveMember_WhenNeverAMember_RespondsOkAsNoOp()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"RemoveNonMember-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);

		var response = await client.DeleteAsync($"v1/organization/{org.Id}/member/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}
}

public static class ClientOrganizationMemberExtensions
{
	public static async Task<Org.Response.Organization> CreateMyOrganization(this HttpClient client, string name)
	{
		var response = await client.PostAsJsonAsync("v1/organization", new CreateOrganizationRequest { Name = name });
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<Org.Response.Organization>())!;
	}

	public static async Task<List<OrganizationMembership>> GetMyOrganizations(this HttpClient client)
	{
		var response = await client.GetAsync("v1/organization");
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<List<OrganizationMembership>>())!;
	}
}
