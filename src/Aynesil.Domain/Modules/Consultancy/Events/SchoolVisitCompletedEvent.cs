namespace Aynesil.Domain.Modules.Consultancy.Events;

public record SchoolVisitCompletedEvent(
    Guid VisitId,
    Guid CorporationId,
    Guid InstitutionId,
    DateOnly VisitDate) : BaseDomainEvent;
