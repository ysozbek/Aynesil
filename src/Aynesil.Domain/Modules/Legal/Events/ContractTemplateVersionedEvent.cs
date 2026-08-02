namespace Aynesil.Domain.Modules.Legal.Events;

public record ContractTemplateVersionedEvent(
    Guid NewTemplateId,
    Guid CorporationId,
    string Code,
    int NewVersion) : BaseDomainEvent;
