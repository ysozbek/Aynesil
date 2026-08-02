namespace Aynesil.Domain.Modules.Camps.Events;

public record CampEducatorAssignedEvent(
    Guid AssignmentId,
    Guid CorporationId,
    Guid CampId,
    Guid EducatorId,
    string Role) : BaseDomainEvent;
