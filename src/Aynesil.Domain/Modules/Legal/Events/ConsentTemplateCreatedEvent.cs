namespace Aynesil.Domain.Modules.Legal.Events;

public record ConsentTemplateCreatedEvent(
    Guid TemplateId,
    Guid CorporationId,
    string Code,
    int Version) : BaseDomainEvent;
