namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.ref_value_tenant_override. Composite PK (corporation_id, ref_value_id).
/// Allows a tenant to deactivate, reorder, or re-default a SYSTEM or GLOBAL ref value
/// without mutating the shared row (which other tenants depend on).
/// Only nullable columns represent "no override" (null = use the shared value's setting).
/// </summary>
public class RefValueTenantOverride
{
    public Guid CorporationId { get; set; }
    public Guid RefValueId { get; set; }

    /// <summary>Null = no override; false = hidden from this tenant's UI.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Null = no override; true = this is the tenant's default for the category.</summary>
    public bool? IsDefault { get; set; }

    /// <summary>Null = no override; set = this tenant's sort order for the value.</summary>
    public int? SortOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }

    public RefValue? RefValue { get; set; }
}
