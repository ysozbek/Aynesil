namespace Aynesil.Domain.Modules.Iam.Events;

/// <summary>
/// Raised when a user's password is changed (by the user or by an admin reset).
/// Downstream handlers should revoke all existing refresh token sessions for this user.
/// </summary>
public record UserPasswordChangedEvent(
    Guid UserId,
    Guid CorporationId) : BaseDomainEvent;
