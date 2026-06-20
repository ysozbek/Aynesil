namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when an assessment session transitions from 'planned' to 'in_progress'.
/// Consumers: audit log, dashboard refresh.
/// </summary>
public record AssessmentSessionStartedEvent(
    Guid SessionId,
    Guid CorporationId,
    Guid? StartedBy) : BaseDomainEvent;
