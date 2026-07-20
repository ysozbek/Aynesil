namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting.
/// An internal, parent, prospect, or external meeting.
/// MeetingTypeId references ref_value(meeting_type) — configurable, not hardcoded.
/// Status: 'scheduled' | 'completed' | 'cancelled'.
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
}
