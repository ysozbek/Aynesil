using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_activity.
/// A planned activity within a camp period.
/// ActivityTypeId → ref_value(camp_activity_type): therapy | sports | social | educational.
/// Optional SessionId bridges to scheduling.session for calendar reuse.
/// </summary>
public class CampActivity : TenantEntity
{
    public Guid CampPeriodId { get; private set; }

    /// <summary>FK to ref_value(camp_activity_type).</summary>
    public Guid? ActivityTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public string? Location { get; private set; }
    public int? Capacity { get; private set; }

    /// <summary>Optional FK to scheduling.session.</summary>
    public Guid? SessionId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public CampPeriod Period { get; private set; } = null!;
    public ICollection<CampActivityParticipation> Participations { get; private set; } = [];
    public ICollection<CampEducator> Educators { get; private set; } = [];

    public static CampActivity Create(
        Guid corporationId,
        Guid campPeriodId,
        string name,
        Guid? activityTypeId = null,
        string? description = null,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null,
        string? location = null,
        int? capacity = null,
        Guid? sessionId = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Activity name is required.", nameof(name));
        if (endsAt.HasValue && startsAt.HasValue && endsAt <= startsAt)
            throw new ArgumentException("Activity ends_at must be after starts_at.");
        if (capacity is <= 0)
            throw new ArgumentException("Capacity must be a positive integer.", nameof(capacity));

        var activity = new CampActivity
        {
            CorporationId  = corporationId,
            CampPeriodId   = campPeriodId,
            ActivityTypeId = activityTypeId,
            Name           = name.Trim(),
            Description    = description?.Trim(),
            StartsAt       = startsAt,
            EndsAt         = endsAt,
            Location       = location?.Trim(),
            Capacity       = capacity,
            SessionId      = sessionId,
            IsActive       = true,
            CreatedBy      = createdBy
        };

        activity.AddDomainEvent(new CampActivityCreatedEvent(
            activity.Id, corporationId, campPeriodId, activity.Name));

        return activity;
    }

    public void Update(
        string name,
        Guid? activityTypeId,
        string? description,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        string? location,
        int? capacity,
        Guid? sessionId,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Activity name is required.", nameof(name));
        if (endsAt.HasValue && startsAt.HasValue && endsAt <= startsAt)
            throw new ArgumentException("Activity ends_at must be after starts_at.");
        if (capacity is <= 0)
            throw new ArgumentException("Capacity must be a positive integer.", nameof(capacity));

        Name           = name.Trim();
        ActivityTypeId = activityTypeId;
        Description    = description?.Trim();
        StartsAt       = startsAt;
        EndsAt         = endsAt;
        Location       = location?.Trim();
        Capacity       = capacity;
        SessionId      = sessionId;
        UpdatedAt      = DateTimeOffset.UtcNow;
        UpdatedBy      = updatedBy;
    }

    public void Activate(Guid? updatedBy = null)
    {
        if (IsActive)
            throw new InvalidOperationException("Activity is already active.");
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Activity is already inactive.");
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
