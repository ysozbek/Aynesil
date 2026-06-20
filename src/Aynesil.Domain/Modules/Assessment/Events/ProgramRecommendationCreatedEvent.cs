namespace Aynesil.Domain.Modules.Assessment.Events;

/// <summary>
/// Raised when a program recommendation is created from an assessment.
/// Consumers: enrollment module (pre-fill enrollment form with recommended program),
/// CRM (advance lead pipeline stage to 'recommendation received'), audit log.
/// </summary>
public record ProgramRecommendationCreatedEvent(
    Guid RecommendationId,
    Guid CorporationId,
    Guid? AssessmentSessionId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? RecommendedProgramId) : BaseDomainEvent;
