namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when a makeup request is created for a missed session.
/// </summary>
public record MakeupRequestedEvent(
    Guid MakeupRequestId,
    Guid CorporationId,
    Guid StudentId,
    Guid? MissedSessionId,
    Guid? RequestedBy) : BaseDomainEvent;
