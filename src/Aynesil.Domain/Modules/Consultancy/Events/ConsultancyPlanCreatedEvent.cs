namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyPlanCreatedEvent(
    Guid PlanId,
    Guid CorporationId,
    Guid InstitutionId,
    string Name) : BaseDomainEvent;
