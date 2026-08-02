namespace Aynesil.Domain.Modules.Consultancy.Events;

public record InstitutionCreatedEvent(
    Guid InstitutionId,
    Guid CorporationId,
    string Name) : BaseDomainEvent;
