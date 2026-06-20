namespace Aynesil.Domain.Events;

/// <summary>
/// Implemented by all domain entities that can raise domain events.
/// BaseEntity implements this by default so all entities participate.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void RemoveDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
