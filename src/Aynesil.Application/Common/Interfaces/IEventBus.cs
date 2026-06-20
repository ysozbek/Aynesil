using Aynesil.Domain.Events;

namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Integration event bus abstraction.
/// Current implementation: in-process (MediatR IPublisher).
/// Future migration path: replace with a distributed message broker (Kafka, RabbitMQ, SQS)
/// by implementing this interface with a message producer.
/// The transactional outbox (core.outbox_event) ensures exactly-once delivery semantics
/// when the future distributed implementation is wired in — no business code changes needed.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes a domain event in-process (current implementation).</summary>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);

    /// <summary>
    /// Writes an integration event to the transactional outbox for reliable external dispatch.
    /// The outbox processor picks up pending events and publishes them to external brokers.
    /// </summary>
    Task PublishToOutboxAsync(
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        object payload,
        CancellationToken ct = default);
}
