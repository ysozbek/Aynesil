namespace Aynesil.Domain.Modules.Core.Events;

/// <summary>
/// Raised when a new campus (branch) is created under a corporation.
/// </summary>
public record CampusCreatedEvent(
    Guid CampusId,
    Guid CorporationId,
    string Code,
    string Name) : BaseDomainEvent;
