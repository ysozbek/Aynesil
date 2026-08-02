namespace Aynesil.Domain.Modules.Consultancy.Events;

public record FollowUpActivityCreatedEvent(
    Guid ActivityId,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId) : BaseDomainEvent;
