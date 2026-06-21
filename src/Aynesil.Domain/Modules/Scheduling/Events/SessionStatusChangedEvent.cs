namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when a session's status transitions (e.g. scheduled → completed, scheduled → cancelled).
/// </summary>
public record SessionStatusChangedEvent(
    Guid SessionId,
    Guid CorporationId,
    string PreviousStatus,
    string NewStatus,
    Guid? UpdatedBy) : BaseDomainEvent;
