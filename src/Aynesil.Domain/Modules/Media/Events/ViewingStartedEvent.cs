namespace Aynesil.Domain.Modules.Media.Events;

/// <summary>
/// Raised when a guardian or staff member begins watching a live/replay camera feed.
/// Used for security and audit notification pipelines.
/// </summary>
public record ViewingStartedEvent(
    long ViewingLogId,
    DateTimeOffset StartedAt,
    Guid CorporationId,
    Guid? GuardianId,
    Guid? UserId,
    Guid? SessionId,
    Guid? CameraId,
    Guid? AuthorizationId) : BaseDomainEvent;
