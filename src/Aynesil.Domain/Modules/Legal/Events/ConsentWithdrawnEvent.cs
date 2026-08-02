namespace Aynesil.Domain.Modules.Legal.Events;

/// <summary>
/// Raised when a consent is withdrawn.
/// Downstream handlers must revoke any active viewing authorizations
/// that relied on this consent (KVKK compliance).
/// </summary>
public record ConsentWithdrawnEvent(
    Guid ConsentId,
    Guid CorporationId,
    Guid StudentId,
    Guid? ConsentTypeId) : BaseDomainEvent;
