using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Per-(guardian, student) parent portal visibility configuration.
/// Maps to students.guardian_portal_access.
/// Unique constraint: (guardian_id, student_id).
///
/// Camera access defaults to OFF and requires a separate KVKK consent to enable.
/// Revoke by setting RevokedAt — never physically delete.
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class GuardianPortalAccess : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid GuardianId { get; set; }
    public Guid StudentId { get; set; }

    public bool CanViewSessions { get; set; } = true;
    public bool CanViewAttendance { get; set; } = true;
    public bool CanViewReports { get; set; } = true;
    public bool CanViewPlan { get; set; } = true;
    public bool CanViewFinance { get; set; } = true;

    /// <summary>Camera feed access. Requires KVKK consent. Defaults OFF.</summary>
    public bool CanViewCamera { get; set; }

    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>NULL = currently active. Set to revoke access for this student.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Computed helper — not mapped to DB.</summary>
    public bool IsActive => RevokedAt == null;
}
