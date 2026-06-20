using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// M:N link between a student and a guardian.
/// Carries custody, portal-access, and financial-responsibility flags.
/// Maps to students.student_guardian.
/// Unique constraint: (student_id, guardian_id).
/// No audit columns — inherits only BaseEntity (Id).
/// </summary>
public class StudentGuardian : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type: guardian_relationship). e.g. Mother, Father, Grandparent.</summary>
    public Guid? RelationshipId { get; set; }

    /// <summary>Primary guardian — the first contact for school communications.</summary>
    public bool IsPrimary { get; set; }

    public bool HasCustody { get; set; } = true;

    /// <summary>
    /// When true, this guardian may access the parent portal for this specific student.
    /// The per-feature visibility toggles live in GuardianPortalAccess.
    /// </summary>
    public bool PortalAccess { get; set; }

    public bool FinancialResponsible { get; set; }
}
