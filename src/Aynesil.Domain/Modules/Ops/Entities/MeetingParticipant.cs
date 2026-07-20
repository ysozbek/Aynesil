namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting_participant.
/// A participant in a meeting: a platform user, a guardian, a CRM lead, or an external person.
/// participant_type: 'user' | 'guardian' | 'lead' | 'external'.
/// attendance: 'invited' | 'attended' | 'absent' | 'tentative'.
/// </summary>
public class MeetingParticipant : TenantEntity
{
    public Guid MeetingId { get; private set; }

    /// <summary>'user' | 'guardian' | 'lead' | 'external'.</summary>
    public string ParticipantType { get; private set; } = string.Empty;

    public Guid? UserId { get; private set; }
    public Guid? GuardianId { get; private set; }
    public Guid? LeadId { get; private set; }
    public string? ExternalName { get; private set; }

    /// <summary>'invited' | 'attended' | 'absent' | 'tentative'.</summary>
    public string? Attendance { get; private set; }

    public Meeting? Meeting { get; private set; }
}
