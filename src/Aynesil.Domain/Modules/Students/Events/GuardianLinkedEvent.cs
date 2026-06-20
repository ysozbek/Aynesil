namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a guardian is linked to a student (StudentGuardian record created).
/// Consumers: audit log, portal access provisioning workflow, notification service.
/// </summary>
public record GuardianLinkedEvent(
    Guid StudentId,
    Guid GuardianId,
    Guid CorporationId,
    bool IsPrimary,
    bool PortalAccess,
    Guid? LinkedBy) : BaseDomainEvent;
