using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Scheduling.Events;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// Attendance record for one student in one session.
/// reason_id references ref.ref_value (ref_type 'attendance_reason') — configurable.
/// Unique constraint: (session_id, student_id).
///
/// Maps to scheduling.attendance.
/// Audit: recorded_at, recorded_by (non-standard fields; no row_version / deleted_at in DDL).
/// </summary>
public class Attendance : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid StudentId { get; private set; }

    /// <summary>present | absent | late | excused | left_early</summary>
    public string Status { get; private set; } = "present";

    /// <summary>FK to ref.ref_value (ref_type 'attendance_reason'). Configurable.</summary>
    public Guid? ReasonId { get; private set; }

    public int? MinutesAttended { get; private set; }

    /// <summary>FK to iam.user_account — the user who recorded the attendance.</summary>
    public Guid? RecordedBy { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string? Note { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Attendance Record(
        Guid corporationId,
        Guid sessionId,
        Guid studentId,
        string status,
        Guid? reasonId = null,
        int? minutesAttended = null,
        string? note = null,
        Guid? recordedBy = null)
    {
        ValidateStatus(status);

        var attendance = new Attendance
        {
            CorporationId  = corporationId,
            SessionId      = sessionId,
            StudentId      = studentId,
            Status         = status,
            ReasonId       = reasonId,
            MinutesAttended = minutesAttended,
            Note           = note,
            RecordedBy     = recordedBy,
            RecordedAt     = DateTimeOffset.UtcNow
        };

        attendance.AddDomainEvent(new AttendanceRecordedEvent(
            attendance.Id, corporationId, sessionId, studentId, status));

        return attendance;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Correct(
        string status,
        Guid? reasonId,
        int? minutesAttended,
        string? note,
        Guid? correctedBy = null)
    {
        ValidateStatus(status);

        Status          = status;
        ReasonId        = reasonId;
        MinutesAttended = minutesAttended;
        Note            = note;
        RecordedBy      = correctedBy;
        RecordedAt      = DateTimeOffset.UtcNow;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidStatuses =
        ["present", "absent", "late", "excused", "left_early"];

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid attendance status '{status}'. Must be present, absent, late, excused, or left_early.");
    }
}
