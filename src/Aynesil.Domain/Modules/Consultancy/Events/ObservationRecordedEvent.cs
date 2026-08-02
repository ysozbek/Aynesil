namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ObservationRecordedEvent(
    Guid ObservationId,
    Guid CorporationId,
    Guid SchoolVisitId) : BaseDomainEvent;
