namespace Aynesil.Domain.Modules.Students.Events;

/// <summary>
/// Raised when a student's lifecycle status changes (e.g. Active → Passive, Passive → Active).
/// Consumers: audit log, notification service, reporting aggregates.
/// </summary>
public record StudentStatusChangedEvent(
    Guid StudentId,
    Guid CorporationId,
    Guid? PreviousStatusId,
    Guid NewStatusId,
    Guid? ChangedBy) : BaseDomainEvent;
