namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when a makeup session is assigned to a makeup request.
/// </summary>
public record MakeupScheduledEvent(
    Guid MakeupRequestId,
    Guid CorporationId,
    Guid StudentId,
    Guid MakeupSessionId) : BaseDomainEvent;
