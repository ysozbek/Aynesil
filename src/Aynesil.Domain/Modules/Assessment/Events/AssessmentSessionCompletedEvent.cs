namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when an assessment session is completed and scored.
/// Consumers: CRM (trigger next pipeline stage), notification service, audit log,
/// reporting aggregation, outbox relay (future distributed events).
/// </summary>
public record AssessmentSessionCompletedEvent(
    Guid SessionId,
    Guid CorporationId,
    Guid? LeadId,
    Guid? StudentId,
    decimal? TotalScore,
    Guid? CompletedBy) : BaseDomainEvent;
