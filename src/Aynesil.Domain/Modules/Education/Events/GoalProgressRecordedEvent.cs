namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a progress measurement is recorded for a student goal.
/// Consumers: audit log, trend analysis engine, notification service (milestone alerts).
/// </summary>
public record GoalProgressRecordedEvent(
    Guid GoalProgressId,
    Guid CorporationId,
    Guid StudentGoalId,
    DateOnly MeasuredOn,
    decimal? PercentComplete,
    string? Trend) : BaseDomainEvent;
