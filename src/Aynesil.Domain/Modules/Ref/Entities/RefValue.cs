namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.ref_value.
/// Entries within a reference-data category.
/// CorporationId nullable: NULL = system/global value; set = tenant-specific value.
/// Tenants can add their own values when ref_type.allows_tenant_values = true.
/// Shared (system/global) rows cannot be deleted by tenants — they use
/// ref_value_tenant_override to deactivate/reorder without mutating the shared row.
/// </summary>
public class RefValue : BaseEntity
{
    public Guid RefTypeId { get; set; }

    /// <summary>
    /// NULL = system or global value visible to all tenants.
    /// Set = tenant-specific value visible only to that corporation.
    /// </summary>
    public Guid? CorporationId { get; set; }

    /// <summary>Stable machine code within the category. Unique per (ref_type, scope).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Parent value for hierarchical categories (e.g. diagnosis group → subcategory).</summary>
    public Guid? ParentValueId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    /// <summary>True = platform-owned; tenants cannot delete this row.</summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Type-specific attributes as JSON. Shape validated against ref_type.value_schema.
    /// Examples: {"default_duration_minutes": 45, "color": "#4A90D9"} for session_type.
    /// </summary>
    public string Metadata { get; set; } = "{}";

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
    public int RowVersion { get; set; } = 1;

    public RefType? RefType { get; set; }
    public RefValue? ParentValue { get; set; }
    public ICollection<RefValue> ChildValues { get; set; } = [];
    public ICollection<RefValueTranslation> Translations { get; set; } = [];
    public ICollection<RefValueTenantOverride> TenantOverrides { get; set; } = [];
}
