using Api.Org.Model;
using Api.Org.Model.Events;

namespace Api.Test;

public sealed class OrganizationModelTest
{
	static readonly Guid OrgId = Guid.NewGuid();

	[Test]
	public async Task Create_HasNoMembers()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));

		await Assert.That(org.Members).IsEmpty();
	}

	[Test]
	public async Task Apply_MemberAdded_AddsMemberWithRole()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));

		var updated = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Admin), org);

		await Assert.That(updated.Members).ContainsKey("user-1");
		await Assert.That(updated.Members["user-1"]).IsEqualTo(OrganizationRole.Admin);
	}

	[Test]
	public async Task Apply_MemberAdded_ForExistingMember_OverwritesRole()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));
		org = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Member), org);

		var updated = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Admin), org);

		await Assert.That(updated.Members["user-1"]).IsEqualTo(OrganizationRole.Admin);
		await Assert.That(updated.Members.Count).IsEqualTo(1);
	}

	[Test]
	public async Task Apply_MemberAdded_DoesNotMutatePreviousInstance()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));

		Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Admin), org);

		await Assert.That(org.Members).IsEmpty();
	}

	[Test]
	public async Task Apply_MemberRemoved_RemovesMemberAndLeavesOthers()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));
		org = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Admin), org);
		org = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-2", OrganizationRole.Member), org);

		var updated = Organization.Apply(new OrganizationMemberRemoved(OrgId, "user-1", OrganizationRole.Admin), org);

		await Assert.That(updated.Members).DoesNotContainKey("user-1");
		await Assert.That(updated.Members).ContainsKey("user-2");
	}

	[Test]
	public async Task Apply_MemberRemoved_ForNonMember_IsNoOp()
	{
		var org = Organization.Create(new OrganizationCreated(OrgId, "Acme"));
		org = Organization.Apply(new OrganizationMemberAdded(OrgId, "user-1", OrganizationRole.Admin), org);

		var updated = Organization.Apply(new OrganizationMemberRemoved(OrgId, "user-2", OrganizationRole.Member), org);

		await Assert.That(updated.Members.Count).IsEqualTo(1);
		await Assert.That(updated.Members).ContainsKey("user-1");
	}
}

public sealed class OrganizationInviteModelTest
{
	[Test]
	public async Task Create_SetsIdOrganizationIdAndEmail()
	{
		var orgId = Guid.NewGuid();
		var inviteId = Guid.NewGuid();

		var invite = OrganizationInvite.Create(new OrganizationInviteCreated(inviteId, orgId, "someone@example.com"));

		await Assert.That(invite.Id).IsEqualTo(inviteId);
		await Assert.That(invite.OrganizationId).IsEqualTo(orgId);
		await Assert.That(invite.Email).IsEqualTo("someone@example.com");
	}

	[Test]
	public async Task Create_HasNoAcceptedByUserId()
	{
		var invite = OrganizationInvite.Create(new OrganizationInviteCreated(Guid.NewGuid(), Guid.NewGuid(), "someone@example.com"));

		await Assert.That(invite.AcceptedByUserId).IsNull();
	}

	[Test]
	public async Task Apply_InviteAccepted_SetsAcceptedByUserId()
	{
		var invite = OrganizationInvite.Create(new OrganizationInviteCreated(Guid.NewGuid(), Guid.NewGuid(), "someone@example.com"));

		var updated = OrganizationInvite.Apply(new OrganizationInviteAccepted(invite.Id, "user-1"), invite);

		await Assert.That(updated.AcceptedByUserId).IsEqualTo("user-1");
	}
}
