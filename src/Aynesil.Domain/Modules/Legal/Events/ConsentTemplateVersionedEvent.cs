namespace Aynesil.Domain.Modules.Legal.Events;

public record ConsentTemplateVersionedEvent(
    Guid NewTemplateId,
    Guid CorporationId,
    string Code,
    int NewVersion) : BaseDomainEvent;
