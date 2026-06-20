using System.Text.Json;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Events;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Infrastructure.Persistence;
using MediatR;

namespace Aynesil.Infrastructure.Events;

/// <summary>
/// In-process event bus backed by MediatR.
/// For distributed future: replace IPublisher with a Kafka/RabbitMQ producer here.
/// The outbox pathway is already wired — zero business code changes needed when
/// migrating to a distributed broker.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly IPublisher _publisher;
    private readonly AynesilDbContext _db;
    private readonly ITenantContext _tenantContext;

    public InProcessEventBus(IPublisher publisher, AynesilDbContext db, ITenantContext tenantContext)
    {
        _publisher = publisher;
        _db = db;
        _tenantContext = tenantContext;
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default) =>
        _publisher.Publish(domainEvent, ct);

    public async Task PublishToOutboxAsync(
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        object payload,
        CancellationToken ct = default)
    {
        _db.OutboxEvents.Add(new OutboxEvent
        {
            CorporationId = _tenantContext.CorporationId,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            Status = "pending",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
