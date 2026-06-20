namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when an assessment session is first scheduled (status = planned).
/// Consumers: notification service (confirm appointment to assessor), audit log.
/// </summary>
public record AssessmentSessionCreatedEvent(
    Guid SessionId,
    Guid CorporationId,
    Guid TemplateId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? CreatedBy) : BaseDomainEvent;
