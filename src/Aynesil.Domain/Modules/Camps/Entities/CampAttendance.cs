using Aynesil.Domain.Modules.Camps.Events;

namespace Aynesil.Domain.Modules.Camps.Entities;

/// <summary>
/// Maps to camps.camp_attendance.
/// Daily attendance record for one enrolled student.
/// Status: 'present' | 'absent' | 'late' | 'excused'.
/// ReasonId references ref_value(attendance_reason) — configurable.
/// The DB schema has no standard audit columns — this entity extends BaseEntity only.
/// </summary>
public class CampAttendance : BaseEntity
{
    private static readonly string[] ValidStatuses = ["present", "absent", "late", "excused"];

    public Guid CorporationId { get; private set; }
    public Guid CampEnrollmentId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }

    /// <summary>'present' | 'absent' | 'late' | 'excused'</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>FK to ref_value(attendance_reason). Required when status is 'absent' or 'excused'.</summary>
    public Guid? ReasonId { get; private set; }

    public Guid? RecordedBy { get; private set; }

    public CampEnrollment Enrollment { get; private set; } = null!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static CampAttendance Record(
        Guid corporationId,
        Guid campEnrollmentId,
        DateOnly attendanceDate,
        string status,
        Guid? reasonId = null,
        Guid? recordedBy = null)
    {
        ValidateStatus(status);

        var attendance = new CampAttendance
        {
            CorporationId    = corporationId,
            CampEnrollmentId = campEnrollmentId,
            AttendanceDate   = attendanceDate,
            Status           = status,
            ReasonId         = reasonId,
            RecordedBy       = recordedBy
        };

        attendance.AddDomainEvent(new CampAttendanceRecordedEvent(
            attendance.Id, campEnrollmentId, corporationId, attendanceDate, status));

        return attendance;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(string status, Guid? reasonId, Guid? updatedBy)
    {
        ValidateStatus(status);
        Status     = status;
        ReasonId   = reasonId;
        RecordedBy = updatedBy ?? RecordedBy;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid attendance status '{status}'. Valid: {string.Join(", ", ValidStatuses)}");
    }
}
