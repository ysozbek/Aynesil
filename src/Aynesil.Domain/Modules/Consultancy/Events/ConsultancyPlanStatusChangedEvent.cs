namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyPlanStatusChangedEvent(
    Guid PlanId,
    Guid CorporationId,
    string OldStatus,
    string NewStatus) : BaseDomainEvent;
