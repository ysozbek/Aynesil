using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Assignment of a student to a specific program (within an optional enrollment).
/// Status is a checked text column: active | paused | completed | cancelled.
/// Maps to education.student_program.
///
/// Audit: created_at, updated_at only (created_by/updated_by absent from DDL — see config).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class StudentProgram : TenantEntity
{
    public Guid StudentId { get; private set; }
    public Guid ProgramId { get; private set; }

    /// <summary>Optional parent enrollment record.</summary>
    public Guid? EnrollmentId { get; private set; }

    /// <summary>FK to core.campus — campus where this program is delivered.</summary>
    public Guid? CampusId { get; private set; }

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    /// <summary>Allowed values: active | paused | completed | cancelled. Validated in commands.</summary>
    public string Status { get; private set; } = "active";

    // ── Navigations ───────────────────────────────────────────────────────────

    public EducationProgram Program { get; private set; } = null!;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static StudentProgram Create(
        Guid corporationId,
        Guid studentId,
        Guid programId,
        Guid? enrollmentId = null,
        Guid? campusId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var sp = new StudentProgram
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            ProgramId     = programId,
            EnrollmentId  = enrollmentId,
            CampusId      = campusId,
            StartDate     = startDate,
            EndDate       = endDate,
            Status        = "active",
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };

        sp.AddDomainEvent(new StudentAssignedToProgramEvent(
            sp.Id, corporationId, studentId, programId, enrollmentId, campusId));

        return sp;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void ChangeStatus(string newStatus)
    {
        string[] allowed = ["active", "paused", "completed", "cancelled"];
        if (!allowed.Contains(newStatus))
            throw new ArgumentException($"Invalid student program status: '{newStatus}'.");

        Status    = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDates(DateOnly? startDate, DateOnly? endDate)
    {
        StartDate = startDate;
        EndDate   = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
