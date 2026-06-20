namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.ref_type.
/// Catalog of reference-data categories. Each row defines one business list.
/// Adding a new business list is an INSERT here — never a schema change.
/// Examples: 'session_type', 'therapy_type', 'payment_method'.
/// </summary>
public class RefType : BaseEntity
{
    /// <summary>Stable machine code. E.g. 'session_type', 'notification_channel'.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Platform engine depends on system-flagged types; tenants cannot delete them.</summary>
    public bool IsSystem { get; set; }

    /// <summary>True if ref_value.parent_value_id is meaningful (taxonomy/hierarchy).</summary>
    public bool IsHierarchical { get; set; }

    /// <summary>False = only platform team can add values (e.g. notification_channel).</summary>
    public bool AllowsTenantValues { get; set; } = true;

    /// <summary>Optional JSON-Schema that validates ref_value.metadata for this category.</summary>
    public string? ValueSchema { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public ICollection<RefValue> Values { get; set; } = [];
}
