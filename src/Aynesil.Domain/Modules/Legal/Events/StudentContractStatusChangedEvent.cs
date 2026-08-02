namespace Aynesil.Domain.Modules.Legal.Events;

public record StudentContractStatusChangedEvent(
    Guid ContractId,
    Guid CorporationId,
    Guid StudentId,
    string OldStatus,
    string NewStatus) : BaseDomainEvent;
