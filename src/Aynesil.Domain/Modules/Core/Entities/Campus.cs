using Aynesil.Domain.Modules.Core.Events;

namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.campus (referred to as "branch" in business/UI terminology).
/// Campus is an authorization sub-scope, NOT an isolation boundary.
/// Multiple campuses share the same corporation_id and the same RLS tenant context.
/// RBAC grants can be scoped to a campus (iam.user_role.campus_id).
/// </summary>
public class Campus : TenantEntity
{
    /// <summary>Machine code, unique within a corporation. Stored uppercase.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Overrides corporation timezone when set. Falls back to corporation.timezone.</summary>
    public string? Timezone { get; set; }

    public decimal? GeoLat { get; set; }
    public decimal? GeoLng { get; set; }

    public bool IsActive { get; set; } = true;

    public Corporation? Corporation { get; set; }

    public static Campus Create(
        Guid corporationId,
        string code,
        string name,
        string? city = null,
        string? addressLine = null,
        string? district = null,
        string? phone = null,
        string? email = null,
        string? timezone = null)
    {
        var campus = new Campus
        {
            CorporationId = corporationId,
            Code = code.ToUpperInvariant(),
            Name = name,
            City = city,
            AddressLine = addressLine,
            District = district,
            Phone = phone,
            Email = email,
            Timezone = timezone
        };
        campus.AddDomainEvent(new CampusCreatedEvent(campus.Id, corporationId, campus.Code, campus.Name));
        return campus;
    }

    /// <summary>Re-enables a previously deactivated campus.</summary>
    public void Activate(Guid? updatedBy = null)
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        AddDomainEvent(new CampusStatusChangedEvent(Id, CorporationId, Code, true));
    }

    /// <summary>Deactivates the campus. Existing data is preserved; new operations referencing this campus are blocked.</summary>
    public void Deactivate(Guid? updatedBy = null)
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        AddDomainEvent(new CampusStatusChangedEvent(Id, CorporationId, Code, false));
    }
}
