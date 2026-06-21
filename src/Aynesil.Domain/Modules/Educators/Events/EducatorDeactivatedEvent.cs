namespace Aynesil.Domain.Modules.Educators.Events;

/// <summary>
/// Raised when an educator is deactivated (is_active set to false).
/// Consumers: audit log, scheduling module (pending session reassignment alert),
/// notification service (admin alert).
/// </summary>
public record EducatorDeactivatedEvent(
    Guid EducatorId,
    Guid CorporationId,
    Guid? DeactivatedBy) : BaseDomainEvent;
