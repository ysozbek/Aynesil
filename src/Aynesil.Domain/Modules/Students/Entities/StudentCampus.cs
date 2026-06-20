using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Multi-campus enrollment link. A student may receive services at several campuses
/// within the same corporation. Unique per (student_id, campus_id).
/// Maps to students.student_campus.
/// No soft delete — end enrollment by setting active_to.
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class StudentCampus : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>FK to core.campus.</summary>
    public Guid CampusId { get; set; }

    public bool IsPrimary { get; set; }

    public DateOnly ActiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>NULL means currently active. Set to close out a campus enrollment.</summary>
    public DateOnly? ActiveTo { get; set; }

    /// <summary>Computed helper — not stored in DB.</summary>
    public bool IsActive => ActiveTo == null || ActiveTo >= DateOnly.FromDateTime(DateTime.UtcNow);
}
