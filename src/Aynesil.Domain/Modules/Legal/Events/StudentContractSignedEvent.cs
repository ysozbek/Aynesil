namespace Aynesil.Domain.Modules.Legal.Events;

/// <summary>
/// Raised when a student contract transitions to 'signed'.
/// Compliance and audit handlers may attach to this event (e-signature provider callback,
/// PDF archival, notification to guardian, etc.).
/// </summary>
public record StudentContractSignedEvent(
    Guid ContractId,
    Guid CorporationId,
    Guid StudentId,
    string SignatureMethod) : BaseDomainEvent;
