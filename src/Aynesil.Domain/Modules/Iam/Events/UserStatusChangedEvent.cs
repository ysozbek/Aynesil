namespace Aynesil.Domain.Modules.Iam.Events;

/// <summary>
/// Raised when a user account transitions between status values
/// ('invited' → 'active', 'active' → 'suspended', etc.).
/// Downstream handlers can revoke active sessions when account is disabled/suspended.
/// </summary>
public record UserStatusChangedEvent(
    Guid UserId,
    Guid CorporationId,
    string PreviousStatus,
    string NewStatus) : BaseDomainEvent;
