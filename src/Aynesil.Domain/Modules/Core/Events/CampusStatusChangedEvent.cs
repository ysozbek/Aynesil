namespace Aynesil.Domain.Modules.Core.Events;

/// <summary>
/// Raised when a campus (branch) is activated or deactivated.
/// </summary>
public record CampusStatusChangedEvent(
    Guid CampusId,
    Guid CorporationId,
    string Code,
    bool IsActive) : BaseDomainEvent;
