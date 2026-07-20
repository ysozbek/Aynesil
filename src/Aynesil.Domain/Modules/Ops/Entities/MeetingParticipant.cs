namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting_participant.
/// A participant in a meeting: a platform user, a guardian, a CRM lead, or an external person.
/// participant_type: 'user' | 'guardian' | 'lead' | 'external'  (hardcoded in DDL CHECK constraint).
/// attendance: 'invited' | 'attended' | 'absent' | 'tentative'.
/// DDL note: no audit columns beyond corporation_id — all audit fields are ignored in EF config.
/// </summary>
public class MeetingParticipant : TenantEntity
{
    private static readonly string[] ValidTypes       = ["user", "guardian", "lead", "external"];
    private static readonly string[] ValidAttendances = ["invited", "attended", "absent", "tentative"];

    public Guid MeetingId { get; private set; }

    /// <summary>'user' | 'guardian' | 'lead' | 'external'.</summary>
    public string ParticipantType { get; private set; } = string.Empty;

    public Guid? UserId       { get; private set; }
    public Guid? GuardianId   { get; private set; }
    public Guid? LeadId       { get; private set; }
    public string? ExternalName { get; private set; }

    /// <summary>'invited' | 'attended' | 'absent' | 'tentative'.</summary>
    public string? Attendance { get; private set; }

    public Meeting? Meeting { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MeetingParticipant Create(
        Guid corporationId,
        Guid meetingId,
        string participantType,
        Guid? userId = null,
        Guid? guardianId = null,
        Guid? leadId = null,
        string? externalName = null,
        string attendance = "invited")
    {
        if (!ValidTypes.Contains(participantType))
            throw new ArgumentException(
                $"Invalid participant_type '{participantType}'. Must be: user, guardian, lead, external.");

        if (!ValidAttendances.Contains(attendance))
            throw new ArgumentException(
                $"Invalid attendance '{attendance}'. Must be: invited, attended, absent, tentative.");

        return new MeetingParticipant
        {
            CorporationId   = corporationId,
            MeetingId       = meetingId,
            ParticipantType = participantType,
            UserId          = userId,
            GuardianId      = guardianId,
            LeadId          = leadId,
            ExternalName    = externalName,
            Attendance      = attendance
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void UpdateAttendance(string attendance)
    {
        if (!ValidAttendances.Contains(attendance))
            throw new ArgumentException(
                $"Invalid attendance '{attendance}'. Must be: invited, attended, absent, tentative.");

        Attendance = attendance;
    }
}
