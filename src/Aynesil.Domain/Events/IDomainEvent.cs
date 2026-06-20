namespace Aynesil.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events are raised inside domain entities/aggregates when something
/// significant happens. They are dispatched after the transaction commits
/// (post-SaveChanges) via MediatR in the application layer.
/// For future distributed scenarios these events are also written to
/// core.outbox_event so they can be reliably published via an outbox processor.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
