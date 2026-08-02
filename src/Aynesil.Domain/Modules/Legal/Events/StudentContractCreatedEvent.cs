namespace Aynesil.Domain.Modules.Legal.Events;

public record StudentContractCreatedEvent(
    Guid ContractId,
    Guid CorporationId,
    Guid StudentId) : BaseDomainEvent;
