namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a coordinator approves an education plan.
/// Consumers: audit log, guardian portal (if guardian_visible=true), notification service.
/// </summary>
public record EducationPlanApprovedEvent(
    Guid EducationPlanId,
    Guid CorporationId,
    Guid StudentId,
    Guid ApproverId,
    int Version,
    Guid? ApprovedBy) : BaseDomainEvent;
