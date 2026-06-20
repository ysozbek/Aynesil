namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when an assessment session is cancelled.
/// Consumers: notification service (inform assessor and lead contact), audit log.
/// </summary>
public record AssessmentSessionCancelledEvent(
    Guid SessionId,
    Guid CorporationId,
    Guid? CancelledBy) : BaseDomainEvent;
