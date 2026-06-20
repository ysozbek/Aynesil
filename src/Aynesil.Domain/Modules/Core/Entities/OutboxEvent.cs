namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.outbox_event. Append-only. Bigint identity PK.
/// Transactional outbox pattern for reliable external event dispatch.
/// Domain events that need to cross process boundaries (webhooks, future message bus)
/// are written here within the same DB transaction as the business operation.
/// An outbox processor picks up pending events and dispatches them, updating status.
/// This enables future migration to distributed messaging (Kafka, RabbitMQ) without
/// changing the business logic — only the outbox processor changes.
/// </summary>
public class OutboxEvent
{
    public long Id { get; set; }

    public Guid? CorporationId { get; set; }

    /// <summary>Domain aggregate type, e.g. 'Student', 'Session', 'Payment'.</summary>
    public string AggregateType { get; set; } = string.Empty;

    public Guid? AggregateId { get; set; }

    /// <summary>Event type code, e.g. 'StudentEnrolled', 'SessionCompleted'.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Serialized event payload as JSON.</summary>
    public string Payload { get; set; } = "{}";

    /// <summary>'pending', 'dispatched', 'failed'.</summary>
    public string Status { get; set; } = "pending";

    public int Attempts { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DispatchedAt { get; set; }
}
