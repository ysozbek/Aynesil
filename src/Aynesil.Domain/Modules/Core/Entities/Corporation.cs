using Aynesil.Domain.Modules.Core.Events;

namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.corporation. The tenant root entity.
/// Every other tenant-scoped row carries corporation_id FK to this table.
/// RLS uses app.current_corporation_id GUC to filter rows in all tenant tables.
/// Does NOT inherit TenantEntity because it IS the tenant boundary.
/// </summary>
public class Corporation : BaseEntity
{
    /// <summary>Machine slug. Unique platform-wide. E.g. 'akran'.</summary>
    public string Code { get; set; } = string.Empty;

    public string LegalName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Default locale for this corporation. FK to ref.locale.code.</summary>
    public string DefaultLocale { get; set; } = "tr";

    public string DefaultCurrency { get; set; } = "TRY";

    /// <summary>IANA timezone identifier, e.g. 'Europe/Istanbul'.</summary>
    public string Timezone { get; set; } = "Europe/Istanbul";

    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }

    /// <summary>'active', 'suspended', or 'closed'.</summary>
    public string Status { get; set; } = "active";

    /// <summary>Corporation-level configuration as JSON (feature flags, branding, etc.).</summary>
    public string Settings { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
    public int RowVersion { get; set; } = 1;

    public ICollection<Campus> Campuses { get; set; } = [];

    public static Corporation Create(string code, string legalName, string displayName, string locale = "tr")
    {
        var corp = new Corporation
        {
            Code = code.ToLowerInvariant(),
            LegalName = legalName,
            DisplayName = displayName,
            DefaultLocale = locale
        };
        corp.AddDomainEvent(new CorporationCreatedEvent(corp.Id, corp.Code));
        return corp;
    }

    /// <summary>Transitions the corporation to 'active' status.</summary>
    public void Activate(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "active";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new CorporationStatusChangedEvent(Id, Code, previous, Status));
    }

    /// <summary>Transitions the corporation to 'suspended' status.</summary>
    public void Suspend(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "suspended";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new CorporationStatusChangedEvent(Id, Code, previous, Status));
    }

    /// <summary>Transitions the corporation to 'closed' status. Irreversible in the domain.</summary>
    public void Close(Guid? updatedBy = null)
    {
        var previous = Status;
        Status = "closed";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
        if (previous != Status)
            AddDomainEvent(new CorporationStatusChangedEvent(Id, Code, previous, Status));
    }

    /// <summary>Soft-deletes the corporation. Use with extreme care — all tenant data becomes inaccessible.</summary>
    public void SoftDelete(Guid? deletedBy = null)
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = deletedBy ?? UpdatedBy;
    }

    /// <summary>Replaces the free-form settings JSON blob.</summary>
    public void UpdateSettings(string settingsJson, Guid? updatedBy = null)
    {
        Settings = settingsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? UpdatedBy;
    }
}
