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

		var response = await client.PostAsJsonAsync("api/v1/organization", new CreateOrganizationRequest { Name = "Acme" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Created);
	}

	[Test]
	public async Task CreateOrganization_AddsCreatorAsAdminMemberAndGrantsOpenFgaTuple()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PostAsJsonAsync("api/v1/organization", new CreateOrganizationRequest { Name = "Acme" });
		var org = (await response.Content.ReadFromJsonAsync<Org.Response.Organization>())!;
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNotNull();

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
			$"api/v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = newMember, Role = OrganizationRole.Member });

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNotNull();

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
			$"api/v1/organization/{org.Id}/member",
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
			$"api/v1/organization/{unknownId}/member",
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
			$"api/v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = member, Role = OrganizationRole.Member });
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var response = await client.DeleteAsync($"api/v1/organization/{org.Id}/member/{member}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNotNull();
		await Assert.That(projected.Members.ContainsKey(member)).IsFalse();

		var deletedMemberTuple = await App.OpenFga.WasWriteCalled(member, "member", "organization", org.Id.ToString(), delete: true);
		await Assert.That(deletedMemberTuple).IsTrue();
	}

	[Test]
	public async Task RemoveMember_WithUnknownOrganizationId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var unknownId = Guid.NewGuid();
		App.OpenFga.MockCheck(subject, "can_add", "organization", unknownId.ToString(), allowed: true);

		var response = await client.DeleteAsync($"api/v1/organization/{unknownId}/member/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task RemoveMember_WhenNeverAMember_RespondsOkAsNoOp()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"RemoveNonMember-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);

		var response = await client.DeleteAsync($"api/v1/organization/{org.Id}/member/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
	}

	[Test]
	public async Task LeaveOrganization_WhenSoleMember_DeletesOrganizationAndRevokesOpenFgaTuple()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"LeaveAlone-{Guid.NewGuid()}");

		var response = await client.PostAsync($"api/v1/organization/{org.Id}/leave", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNull();

		var revokedAdminTuple = await App.OpenFga.WasWriteCalled(subject, "admin", "organization", org.Id.ToString(), delete: true);
		await Assert.That(revokedAdminTuple).IsTrue();
	}

	[Test]
	public async Task LeaveOrganization_WhenOtherMembersRemain_RemovesOnlyCallerAndKeepsOrganization()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"LeaveWithOthers-{Guid.NewGuid()}");
		var otherMember = Guid.NewGuid().ToString();
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		await client.PostAsJsonAsync(
			$"api/v1/organization/{org.Id}/member",
			new AddMemberRequest { UserId = otherMember, Role = OrganizationRole.Member });
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		var otherClient = App.CreateClient().WithAuthenticatedUser(otherMember);
		var response = await otherClient.PostAsync($"api/v1/organization/{org.Id}/leave", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var afterLeave = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNotNull();
		await Assert.That(afterLeave.Members.ContainsKey(otherMember)).IsFalse();
		await Assert.That(afterLeave.Members.ContainsKey(subject)).IsTrue();

		var revokedMemberTuple = await App.OpenFga.WasWriteCalled(otherMember, "member", "organization", org.Id.ToString(), delete: true);
		await Assert.That(revokedMemberTuple).IsTrue();
	}

	[Test]
	public async Task LeaveOrganization_WhenCallerIsNotAMember_RespondsForbidden()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"LeaveNotMember-{Guid.NewGuid()}");
		var outsider = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());

		var response = await outsider.PostAsync($"api/v1/organization/{org.Id}/leave", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task LeaveOrganization_WithUnknownOrganizationId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PostAsync($"api/v1/organization/{Guid.NewGuid()}/leave", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task GetOrganization_WhenCallerCanView_ReturnsOrganizationWithMembers()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"Details-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_view", "organization", org.Id.ToString(), allowed: true);

		var response = await client.GetAsync($"api/v1/organization/{org.Id}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		var details = (await response.Content.ReadFromJsonAsync<OrganizationDetails>())!;
		await Assert.That(details.Id).IsEqualTo(org.Id);
		await Assert.That(details.Name).IsEqualTo(org.Name);
		await Assert.That(details.Members.Any(m => m.UserId == subject && m.Role == OrganizationRole.Admin)).IsTrue();
	}

	[Test]
	public async Task GetOrganization_WhenCallerCannotView_RespondsForbidden()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"DetailsForbidden-{Guid.NewGuid()}");
		var outsider = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());

		var response = await outsider.GetAsync($"api/v1/organization/{org.Id}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task GetOrganization_WithUnknownId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var unknownId = Guid.NewGuid();
		App.OpenFga.MockCheck(subject, "can_view", "organization", unknownId.ToString(), allowed: true);

		var response = await client.GetAsync($"api/v1/organization/{unknownId}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task CreateInvite_WhenCallerCanAdd_CreatesInviteWithToken()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"Invite-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);

		var response = await client.PostAsJsonAsync(
			$"api/v1/organization/{org.Id}/invite",
			new CreateInviteRequest { Email = "new-member@example.com" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Created);
		var invite = (await response.Content.ReadFromJsonAsync<Org.Response.OrganizationInvite>())!;
		await Assert.That(invite.OrganizationId).IsEqualTo(org.Id);
		await Assert.That(invite.Email).IsEqualTo("new-member@example.com");
		await Assert.That(invite.Id).IsNotEqualTo(Guid.Empty);
	}

	[Test]
	public async Task CreateInvite_WhenCallerCannotAdd_RespondsForbidden()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"InviteForbidden-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: false);

		var response = await client.PostAsJsonAsync(
			$"api/v1/organization/{org.Id}/invite",
			new CreateInviteRequest { Email = "new-member@example.com" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.Forbidden);
	}

	[Test]
	public async Task CreateInvite_WithUnknownOrganizationId_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var unknownId = Guid.NewGuid();
		App.OpenFga.MockCheck(subject, "can_add", "organization", unknownId.ToString(), allowed: true);

		var response = await client.PostAsJsonAsync(
			$"api/v1/organization/{unknownId}/invite",
			new CreateInviteRequest { Email = "new-member@example.com" });

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}
}

public static class ClientOrganizationMemberExtensions
{
	public static async Task<Org.Response.Organization> CreateMyOrganization(this HttpClient client, string name)
	{
		var response = await client.PostAsJsonAsync("api/v1/organization", new CreateOrganizationRequest { Name = name });
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<Org.Response.Organization>())!;
	}

	public static async Task<List<OrganizationMembership>> GetMyOrganizations(this HttpClient client)
	{
		var response = await client.GetAsync("api/v1/organization");
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<List<OrganizationMembership>>())!;
	}
}
