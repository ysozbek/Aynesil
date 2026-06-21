namespace Aynesil.Domain.Modules.Scheduling.Events;

/// <summary>
/// Raised when a new session is scheduled (individual, group, makeup, etc.).
/// </summary>
public record SessionCreatedEvent(
    Guid SessionId,
    Guid CorporationId,
    Guid? CampusId,
    Guid SessionTypeId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsMakeup,
    Guid? CreatedBy) : BaseDomainEvent;
