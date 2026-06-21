namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a student goal transitions to a new status (active → achieved, etc.).
/// Consumers: audit log, notification service, parent portal refresh.
/// </summary>
public record StudentGoalStatusChangedEvent(
    Guid StudentGoalId,
    Guid CorporationId,
    Guid StudentId,
    string PreviousStatus,
    string NewStatus,
    Guid? ChangedBy) : BaseDomainEvent;
