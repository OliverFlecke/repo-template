using System.Net;
using System.Net.Http.Json;

using Api.Org.Endpoint;
using Api.Org.Response;

using Marten;
using Marten.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Test;

public sealed class OrganizationInviteTest
{
	[ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
	public required WebApplicationFactory App { get; init; }

	readonly string subject = Guid.NewGuid().ToString();

	[Test]
	public async Task GetInvite_WithValidToken_ReturnsOrganizationNameAndEmail_EvenWithoutAuthentication()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await client.CreateMyOrganization($"Invitees-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		var invite = await client.CreateInvite(org.Id, "someone@example.com");

		var anonymousClient = App.CreateClient();
		var response = await anonymousClient.GetAsync($"api/v1/invite/{invite.Id}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		var details = (await response.Content.ReadFromJsonAsync<InviteDetails>())!;
		await Assert.That(details.OrganizationId).IsEqualTo(org.Id);
		await Assert.That(details.OrganizationName).IsEqualTo(org.Name);
		await Assert.That(details.Email).IsEqualTo("someone@example.com");
		await Assert.That(details.Accepted).IsFalse();
	}

	[Test]
	public async Task GetInvite_WithUnknownToken_RespondsNotFound()
	{
		var anonymousClient = App.CreateClient();

		var response = await anonymousClient.GetAsync($"api/v1/invite/{Guid.NewGuid()}");

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task AcceptInvite_WhenAuthenticated_AddsCallerAsMemberAndGrantsOpenFgaTuple()
	{
		var owner = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await owner.CreateMyOrganization($"Accept-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		var invite = await owner.CreateInvite(org.Id, "someone@example.com");

		var newMember = Guid.NewGuid().ToString();
		var client = App.CreateClient().WithAuthenticatedUser(newMember);

		var response = await client.PostAsync($"api/v1/invite/{invite.Id}/accept", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.OK);
		await App.Services.DocumentStore().WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));

		using var scope = App.Services.CreateScope();
		var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
		var projected = await Assert.That(await session.Events.FetchLatest<Org.Model.Organization>(org.Id)).IsNotNull();
		await Assert.That(projected.Members[newMember]).IsEqualTo(Org.Model.OrganizationRole.Member);

		var wroteMemberTuple = await App.OpenFga.WasWriteCalled(newMember, "member", "organization", org.Id.ToString());
		await Assert.That(wroteMemberTuple).IsTrue();
	}

	[Test]
	public async Task AcceptInvite_WhenUnauthenticated_RespondsUnauthorized()
	{
		var owner = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await owner.CreateMyOrganization($"AcceptUnauth-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		var invite = await owner.CreateInvite(org.Id, "someone@example.com");

		var anonymousClient = App.CreateClient();
		var response = await anonymousClient.PostAsync($"api/v1/invite/{invite.Id}/accept", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.Unauthorized);
	}

	[Test]
	public async Task AcceptInvite_WithUnknownToken_RespondsNotFound()
	{
		var client = App.CreateClient().WithAuthenticatedUser(subject);

		var response = await client.PostAsync($"api/v1/invite/{Guid.NewGuid()}/accept", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task AcceptInvite_WhenAlreadyAccepted_RespondsConflict()
	{
		var owner = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await owner.CreateMyOrganization($"AcceptTwice-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		var invite = await owner.CreateInvite(org.Id, "someone@example.com");

		var firstClient = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());
		await firstClient.PostAsync($"api/v1/invite/{invite.Id}/accept", null);

		var secondClient = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());
		var response = await secondClient.PostAsync($"api/v1/invite/{invite.Id}/accept", null);

		await Assert.That(response).HasStatusCode(HttpStatusCode.Conflict);
	}

	[Test]
	public async Task GetInvite_AfterAccepted_ReturnsAcceptedTrue()
	{
		var owner = App.CreateClient().WithAuthenticatedUser(subject);
		var org = await owner.CreateMyOrganization($"AcceptThenGet-{Guid.NewGuid()}");
		App.OpenFga.MockCheck(subject, "can_add", "organization", org.Id.ToString(), allowed: true);
		var invite = await owner.CreateInvite(org.Id, "someone@example.com");

		var newMember = App.CreateClient().WithAuthenticatedUser(Guid.NewGuid().ToString());
		await newMember.PostAsync($"api/v1/invite/{invite.Id}/accept", null);

		var response = await App.CreateClient().GetAsync($"api/v1/invite/{invite.Id}");

		var details = (await response.Content.ReadFromJsonAsync<InviteDetails>())!;
		await Assert.That(details.Accepted).IsTrue();
	}
}

public static class ClientInviteExtensions
{
	public static async Task<Org.Response.OrganizationInvite> CreateInvite(this HttpClient client, Guid organizationId, string email)
	{
		var response = await client.PostAsJsonAsync($"api/v1/organization/{organizationId}/invite", new CreateInviteRequest { Email = email });
		response.EnsureSuccessStatusCode();

		return (await response.Content.ReadFromJsonAsync<Org.Response.OrganizationInvite>())!;
	}
}
