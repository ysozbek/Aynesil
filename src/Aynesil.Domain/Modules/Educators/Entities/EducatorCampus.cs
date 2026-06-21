using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Educators.Entities;

/// <summary>
/// Educator-campus assignment. An educator may work across multiple campuses
/// within the same corporation. Unique per (educator_id, campus_id).
/// Maps to educators.educator_campus.
/// No audit columns — inherit only BaseEntity (Id).
/// Close an assignment by setting active_to.
/// </summary>
public class EducatorCampus : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid EducatorId { get; set; }

    /// <summary>FK to core.campus.</summary>
    public Guid CampusId { get; set; }

    /// <summary>Marks the educator's home campus for scheduling and reporting.</summary>
    public bool IsPrimary { get; set; }

    public DateOnly ActiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>NULL = currently active. Set to close the campus assignment.</summary>
    public DateOnly? ActiveTo { get; set; }

    /// <summary>Computed helper — not stored in DB.</summary>
    public bool IsActive => ActiveTo == null || ActiveTo >= DateOnly.FromDateTime(DateTime.UtcNow);
}
