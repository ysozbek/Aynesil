namespace Aynesil.Domain.Modules.Camps.Events;

public record CampActivityCreatedEvent(
    Guid ActivityId,
    Guid CorporationId,
    Guid CampPeriodId,
    string Name) : BaseDomainEvent;
