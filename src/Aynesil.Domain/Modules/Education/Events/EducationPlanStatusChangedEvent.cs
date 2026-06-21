namespace Aynesil.Domain.Modules.Education.Events;

/// <summary>
/// Raised on every education plan status transition.
/// Consumers: audit log, notification service, reporting.
/// </summary>
public record EducationPlanStatusChangedEvent(
    Guid EducationPlanId,
    Guid CorporationId,
    Guid StudentId,
    string PreviousStatus,
    string NewStatus,
    Guid? ChangedBy) : BaseDomainEvent;
