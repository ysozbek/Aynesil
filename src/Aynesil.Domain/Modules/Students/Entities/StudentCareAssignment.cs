using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Effective-dated care-team assignment: an educator assigned to a student
/// with a configurable role, campus scope, and delegation/substitution provenance.
///
/// Maps to students.student_care_assignment (ABAC Phase 2/4).
///
/// Authorization model:
///   - RBAC: care_team:assign / care_team:read gates the assignment management API.
///   - ABAC: students.user_can_access_student() reads active assignments of THIS entity
///     to enforce RESTRICTIVE RLS on all clinical tables (Phase 3).
///
/// Soft removal: ActiveTo is set to now() + Status = "ended". Never hard-deleted.
/// The audit trail of "who could see what, when" must survive.
/// </summary>
public class StudentCareAssignment : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid EducatorId { get; set; }

    /// <summary>Optional campus scope. Null = corporation-wide assignment.</summary>
    public Guid? CampusId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type = care_team_role). Configurable per tenant.</summary>
    public Guid RoleId { get; set; }

    public bool IsPrimary { get; set; }

    /// <summary>active | suspended | ended. Never physically deleted.</summary>
    public string Status { get; set; } = "active";

    /// <summary>Effective start date (inclusive).</summary>
    public DateOnly ActiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Effective end date (exclusive). Null = open-ended.</summary>
    public DateOnly? ActiveTo { get; set; }

    // ── Delegation / substitution provenance (design §7) ──────────────────────

    /// <summary>FK to ref.ref_value (ref_type = care_team_grant_type). Null = permanent.</summary>
    public Guid? GrantTypeId { get; set; }

    /// <summary>Self-FK. Points to the original assignment being delegated or substituted.</summary>
    public Guid? SourceAssignmentId { get; set; }

    /// <summary>FK to iam.user_account — the user who created this grant.</summary>
    public Guid? GrantedBy { get; set; }

    /// <summary>Justification. Required for emergency/delegated grant types (enforced in command handler).</summary>
    public string? Reason { get; set; }
}
