namespace Aynesil.Domain.Modules.Media.Events;

/// <summary>
/// Raised when a guardian is granted time-limited access to a camera feed.
/// Consumers may trigger notifications (e.g. inform the guardian or log to audit).
/// </summary>
public record ViewingAuthorizationGrantedEvent(
    Guid AuthorizationId,
    Guid CorporationId,
    Guid GuardianId,
    Guid StudentId,
    Guid? SessionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo) : BaseDomainEvent;
