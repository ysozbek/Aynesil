namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised when a new BEP/IEP education plan is created in draft status.
/// Consumers: audit log, coordinator notification.
/// </summary>
public record EducationPlanCreatedEvent(
    Guid EducationPlanId,
    Guid CorporationId,
    Guid StudentId,
    string Title,
    Guid? CreatedBy) : BaseDomainEvent;
