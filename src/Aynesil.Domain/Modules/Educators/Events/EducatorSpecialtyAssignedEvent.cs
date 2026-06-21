namespace Aynesil.Domain.Modules.Educators.Events;

/// <summary>
/// Raised when a specialty is added to an educator's profile.
/// Consumers: audit log, search index refresh.
/// </summary>
public record EducatorSpecialtyAssignedEvent(
    Guid EducatorId,
    Guid CorporationId,
    Guid SpecialtyId,
    Guid AssignmentId,
    Guid? AssignedBy) : BaseDomainEvent;
