namespace Aynesil.Domain.Modules.Educators.Events;

/// <summary>
/// Raised when a supervisory hierarchy edge is created between two educators.
/// Consumers: audit log, notification service (inform supervisor), reporting.
/// </summary>
public record EducatorHierarchyLinkedEvent(
    Guid EducatorId,
    Guid SupervisorId,
    Guid CorporationId,
    Guid? RelationshipId,
    Guid? CampusId,
    Guid? LinkedBy) : BaseDomainEvent;
