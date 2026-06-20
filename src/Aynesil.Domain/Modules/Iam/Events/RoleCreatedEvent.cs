namespace Aynesil.Domain.Modules.Iam.Events;

/// <summary>
/// Raised when a new role is created (either a system template or a tenant-scoped role).
/// </summary>
public record RoleCreatedEvent(
    Guid RoleId,
    Guid? CorporationId,
    string Code) : BaseDomainEvent;
