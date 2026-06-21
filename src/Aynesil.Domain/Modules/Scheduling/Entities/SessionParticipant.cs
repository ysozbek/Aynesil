using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A student enrolled in a specific session.
/// One session can have multiple participants (group/camp sessions).
/// Unique constraint: (session_id, student_id).
///
/// Maps to scheduling.session_participant.
/// No audit fields in DDL — lifecycle managed via cascade delete from session.
/// </summary>
public class SessionParticipant : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid? StudentProgramId { get; private set; }

    /// <summary>Default 'student'. Extensible via business rules.</summary>
    public string Role { get; private set; } = "student";

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionParticipant Create(
        Guid corporationId,
        Guid sessionId,
        Guid studentId,
        Guid? studentProgramId = null,
        string role = "student")
        => new()
        {
            CorporationId    = corporationId,
            SessionId        = sessionId,
            StudentId        = studentId,
            StudentProgramId = studentProgramId,
            Role             = role
        };
}
