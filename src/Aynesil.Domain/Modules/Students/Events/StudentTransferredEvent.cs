namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a student's primary campus is changed (campus transfer workflow).
/// The previous enrollment's active_to is set; a new StudentCampus record is created
/// with is_primary = true.
/// Consumers: audit log, scheduling module (re-assign future sessions), notification service.
/// </summary>
public record StudentTransferredEvent(
    Guid StudentId,
    Guid CorporationId,
    Guid PreviousCampusId,
    Guid NewCampusId,
    Guid? TransferredBy) : BaseDomainEvent;
