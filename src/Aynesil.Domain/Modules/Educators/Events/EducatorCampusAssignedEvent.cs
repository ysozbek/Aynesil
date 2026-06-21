namespace Aynesil.Domain.Modules.Educators.Events;

/// <summary>
/// Raised when an educator is assigned to a campus.
/// Consumers: audit log, campus capacity reporting, scheduling module.
/// </summary>
public record EducatorCampusAssignedEvent(
    Guid EducatorId,
    Guid CorporationId,
    Guid CampusId,
    bool IsPrimary,
    Guid? AssignedBy) : BaseDomainEvent;
