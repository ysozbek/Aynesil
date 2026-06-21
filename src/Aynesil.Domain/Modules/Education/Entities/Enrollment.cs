using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Overall enrollment of a student at a campus.
/// Enrollment status is configurable reference data (ref_type 'enrollment_status'):
/// pending, active, completed, withdrawn — or any status a business user defines.
/// One enrollment may have multiple StudentProgram records (a student in multiple programs).
/// Maps to education.enrollment.
///
/// Audit: full (created_at, created_by, updated_at, updated_by).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class Enrollment : TenantEntity
{
    public Guid StudentId { get; private set; }

    /// <summary>FK to core.campus — the campus at which the student is enrolled.</summary>
    public Guid? CampusId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'enrollment_status'). Configurable lifecycle status.</summary>
    public Guid? StatusId { get; private set; }

    public DateOnly EnrolledOn { get; private set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>NULL = still enrolled. Set when enrollment ends.</summary>
    public DateOnly? EndedOn { get; private set; }

    public string? TerminationReason { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<StudentProgram> StudentPrograms { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Enrollment Create(
        Guid corporationId,
        Guid studentId,
        Guid? campusId,
        Guid? statusId,
        DateOnly? enrolledOn = null,
        Guid? createdBy = null)
    {
        var enrollment = new Enrollment
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            CampusId      = campusId,
            StatusId      = statusId,
            EnrolledOn    = enrolledOn ?? DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = createdBy
        };

        enrollment.AddDomainEvent(new StudentEnrolledEvent(
            enrollment.Id, corporationId, studentId, campusId, statusId, createdBy));

        return enrollment;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void ChangeStatus(Guid newStatusId, Guid? updatedBy = null)
    {
        StatusId  = newStatusId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void End(DateOnly endedOn, string? reason, Guid? updatedBy = null)
    {
        EndedOn           = endedOn;
        TerminationReason = reason;
        UpdatedAt         = DateTimeOffset.UtcNow;
        UpdatedBy         = updatedBy;
    }
}
