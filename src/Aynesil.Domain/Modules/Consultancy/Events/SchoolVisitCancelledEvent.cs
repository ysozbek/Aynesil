namespace Aynesil.Domain.Modules.Consultancy.Events;

public record SchoolVisitCancelledEvent(
    Guid VisitId,
    Guid CorporationId,
    Guid InstitutionId,
    DateOnly VisitDate) : BaseDomainEvent;
