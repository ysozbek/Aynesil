namespace Aynesil.Domain.Modules.Consultancy.Events;

public record SchoolVisitScheduledEvent(
    Guid VisitId,
    Guid CorporationId,
    Guid InstitutionId,
    DateOnly VisitDate,
    Guid? VisitorId) : BaseDomainEvent;
