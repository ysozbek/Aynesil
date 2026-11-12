using Aynesil.Domain.Common;
namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting.
/// An internal, parent, prospect, or external meeting.
/// MeetingTypeId references ref_value(meeting_type) — configurable, not hardcoded.
/// Status: 'scheduled' | 'completed' | 'cancelled'.
/// DDL note: updated_at exists but updated_by does not — UpdatedBy is ignored in EF config.
/// </summary>
public class Meeting : TenantEntity
{
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref_value(meeting_type).</summary>
    public Guid? MeetingTypeId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }

    public string? Location { get; private set; }
    public Guid? RoomId { get; private set; }

    /// <summary>'scheduled' | 'completed' | 'cancelled'.</summary>
    public string Status { get; private set; } = "scheduled";

    public Guid? OrganizerId { get; private set; }

    public ICollection<MeetingParticipant> Participants { get; private set; } = [];
    public ICollection<MeetingOutcome> Outcomes { get; private set; } = [];
    public ICollection<MeetingFollowUp> FollowUps { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Meeting Create(
        Guid corporationId,
        string title,
        Guid? meetingTypeId = null,
        DateTimeOffset? scheduledAt = null,
        DateTimeOffset? endsAt = null,
        string? location = null,
        Guid? roomId = null,
        Guid? campusId = null,
        Guid? organizerId = null,
        Guid? createdBy = null)
    {
        if (endsAt.HasValue && scheduledAt.HasValue && endsAt <= scheduledAt)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBeAfter, MsgArg.Field("EndsAt"), MsgArg.Field("ScheduledAt"));

        return new Meeting
        {
            CorporationId  = corporationId,
            Title          = title.Trim(),
            MeetingTypeId  = meetingTypeId,
            ScheduledAt    = scheduledAt,
            EndsAt         = endsAt,
            Location       = location,
            RoomId         = roomId,
            CampusId       = campusId,
            OrganizerId    = organizerId,
            Status         = "scheduled",
            CreatedBy      = createdBy
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(
        string title,
        Guid? meetingTypeId,
        DateTimeOffset? scheduledAt,
        DateTimeOffset? endsAt,
        string? location,
        Guid? roomId,
        Guid? campusId,
        Guid? organizerId)
    {
        if (Status == "cancelled")
            throw new BusinessRuleViolationException(MsgKeys.Rule.MeetingUpdateCancelled);

        if (endsAt.HasValue && scheduledAt.HasValue && endsAt <= scheduledAt)
            throw new InvalidArgumentException(MsgKeys.Validation.MustBeAfter, MsgArg.Field("EndsAt"), MsgArg.Field("ScheduledAt"));

        Title         = title.Trim();
        MeetingTypeId = meetingTypeId;
        ScheduledAt   = scheduledAt;
        EndsAt        = endsAt;
        Location      = location;
        RoomId        = roomId;
        CampusId      = campusId;
        OrganizerId   = organizerId;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status == "cancelled")
            throw new BusinessRuleViolationException(MsgKeys.Rule.MeetingCompleteCancelled);
        if (Status == "completed")
            throw new BusinessRuleViolationException(MsgKeys.Rule.MeetingAlreadyCompleted);

        Status    = "completed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == "completed")
            throw new BusinessRuleViolationException(MsgKeys.Rule.MeetingCancelCompleted);
        if (Status == "cancelled")
            throw new BusinessRuleViolationException(MsgKeys.Rule.MeetingAlreadyCancelled);

        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
