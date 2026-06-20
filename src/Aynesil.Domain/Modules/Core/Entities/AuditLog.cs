namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.audit_log. Append-only. Bigint identity PK.
/// Written by the DB trigger core.audit_trigger() on clinical/financial/legal tables.
/// Range-partitioned by occurred_at (monthly via pg_partman or scheduled job).
/// Read-only from the application layer; never deleted.
/// PK is composite (id, occurred_at) at DB level for partition pruning —
/// EF Core maps only id as the entity key for simplicity.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public Guid? CorporationId { get; set; }

    public string SchemaName { get; set; } = string.Empty;

    public string TableName { get; set; } = string.Empty;

    public Guid? RowId { get; set; }

    /// <summary>'INSERT', 'UPDATE', or 'DELETE'.</summary>
    public string Action { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    /// <summary>JSONB snapshot of the row before the change. Null for INSERTs.</summary>
    public string? OldData { get; set; }

    /// <summary>JSONB snapshot of the row after the change. Null for DELETEs.</summary>
    public string? NewData { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
