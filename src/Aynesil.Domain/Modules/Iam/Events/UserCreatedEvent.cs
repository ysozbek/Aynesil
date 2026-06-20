namespace Aynesil.Domain.Modules.Iam.Events;

/// <summary>
/// Raised when a new user account is created within a corporation.
/// Downstream handlers can send a welcome/invite email, seed default preferences, etc.
/// </summary>
public record UserCreatedEvent(
    Guid UserId,
    Guid CorporationId,
    string Username) : BaseDomainEvent;
