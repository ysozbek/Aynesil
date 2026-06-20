namespace Aynesil.Domain.Modules.Core.Events;

/// <summary>
/// Raised when a corporation's status transitions between 'active', 'suspended', and 'closed'.
/// Consumers may notify users or trigger downstream processes on status changes.
/// </summary>
public record CorporationStatusChangedEvent(
    Guid CorporationId,
    string Code,
    string PreviousStatus,
    string NewStatus) : BaseDomainEvent;
