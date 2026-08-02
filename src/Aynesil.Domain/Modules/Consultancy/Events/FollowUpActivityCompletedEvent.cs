namespace Aynesil.Domain.Modules.Consultancy.Events;

public record FollowUpActivityCompletedEvent(
    Guid ActivityId,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId) : BaseDomainEvent;
