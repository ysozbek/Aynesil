namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.campus (referred to as "branch" in the application).
/// Campus is an authorization sub-scope, NOT an isolation boundary.
/// Multiple campuses share the same corporation_id and the same RLS tenant context.
/// RBAC grants can be scoped to a campus (iam.user_role.campus_id).
/// </summary>
public class Campus : TenantEntity
{
    /// <summary>Machine code, unique within a corporation.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Overrides corporation timezone when set.</summary>
    public string? Timezone { get; set; }

    public decimal? GeoLat { get; set; }
    public decimal? GeoLng { get; set; }

    public bool IsActive { get; set; } = true;

    public Corporation? Corporation { get; set; }
}
