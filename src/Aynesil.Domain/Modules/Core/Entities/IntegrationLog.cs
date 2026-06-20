namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.integration_log. Append-only. Bigint identity PK.
/// Records outbound and inbound HTTP calls to/from integration providers.
/// Range-partitioned by occurred_at for efficient archival.
/// Sensitive response data (e.g. payment tokens) must be masked before writing.
/// </summary>
public class IntegrationLog
{
    public long Id { get; set; }

    public Guid? CorporationId { get; set; }

    /// <summary>FK to core.integration_connection.</summary>
    public Guid? ConnectionId { get; set; }

    /// <summary>'outbound' (we called them) or 'inbound' (they called us/webhook).</summary>
    public string Direction { get; set; } = string.Empty;

    public string? Request { get; set; }

    public string? Response { get; set; }

    public int? StatusCode { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
