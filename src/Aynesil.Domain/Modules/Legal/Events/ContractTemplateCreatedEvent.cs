namespace Aynesil.Domain.Modules.Legal.Events;

public record ContractTemplateCreatedEvent(
    Guid TemplateId,
    Guid CorporationId,
    string Code,
    int Version) : BaseDomainEvent;
