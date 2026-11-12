using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp.
/// A camp definition scoped to a corporation and optionally a campus.
/// CampTypeId references ref_value(camp_type) — configurable, never hardcoded.
/// DB columns created_by / updated_by do not exist — those properties are ignored in EF config.
/// Status-like control is via IsActive (activate/deactivate), and soft-delete via DeletedAt.
/// </summary>
public class Camp : TenantEntity
{
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref_value(camp_type). Examples: summer, winter, weekend, day.</summary>
    public Guid? CampTypeId { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Location { get; private set; }

    /// <summary>Maximum number of students across all periods of this camp.</summary>
    public int? Capacity { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<CampPeriod> Periods { get; private set; } = [];
    public ICollection<CampEducator> Educators { get; private set; } = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static Camp Create(
        Guid corporationId,
        string code,
        string name,
        Guid? campTypeId = null,
        Guid? campusId = null,
        string? description = null,
        string? location = null,
        int? capacity = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Code"));
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Name"));
        if (capacity is <= 0)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBePositive, MsgArg.Field("Capacity"));

        var camp = new Camp
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            CampTypeId    = campTypeId,
            Code          = code.Trim().ToLowerInvariant(),
            Name          = name.Trim(),
            Description   = description?.Trim(),
            Location      = location?.Trim(),
            Capacity      = capacity,
            IsActive      = true,
            CreatedBy     = createdBy
        };

        camp.AddDomainEvent(new CampCreatedEvent(camp.Id, corporationId, camp.Code));
        return camp;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? campTypeId,
        Guid? campusId,
        string? description,
        string? location,
        int? capacity,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Name"));
        if (capacity is <= 0)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBePositive, MsgArg.Field("Capacity"));

        Name        = name.Trim();
        CampTypeId  = campTypeId;
        CampusId    = campusId;
        Description = description?.Trim();
        Location    = location?.Trim();
        Capacity    = capacity;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;
    }

    public void Activate(Guid? updatedBy = null)
    {
        if (IsActive)
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampAlreadyActive);

        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        if (!IsActive)
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampAlreadyInactive);

        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
