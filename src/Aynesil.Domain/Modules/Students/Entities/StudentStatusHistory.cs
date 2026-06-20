using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Immutable append-only record of a student status transition.
/// Maps to students.student_status_history.
/// Never updated or soft-deleted — historical integrity is mandatory.
/// DB columns: id, corporation_id, student_id, status_id, reason, changed_at, changed_by.
/// No audit/soft-delete columns — inherits only BaseEntity (Id).
/// </summary>
public class StudentStatusHistory : BaseEntity
{
    public Guid CorporationId { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type: student_status). Configurable.</summary>
    public Guid StatusId { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>FK to iam.user_account — staff member who made the change.</summary>
    public Guid? ChangedBy { get; set; }
}
