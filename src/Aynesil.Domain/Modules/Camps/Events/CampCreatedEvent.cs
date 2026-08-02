namespace Aynesil.Domain.Modules.Camps.Events;

public record CampCreatedEvent(Guid CampId, Guid CorporationId, string Code) : BaseDomainEvent;
