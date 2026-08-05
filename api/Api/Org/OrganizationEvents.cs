namespace Api.Org.Model.Events;

public sealed record OrganizationCreated(Guid Id, string Name);

public sealed record OrganizationUpdated(Guid Id, string Name);

public sealed record OrganizationDeleted(Guid Id);