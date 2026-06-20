using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Clinical or operational case note for a student.
/// Maps to students.case_note.
///
/// Confidential notes are visible only to clinical staff roles (enforced at query/API layer).
/// authored_by is the staff member who wrote the note — stored separately from the
/// AuditableEntity.CreatedBy because case notes can be written on behalf of another person.
///
/// Audit: created_at, updated_at, deleted_at, row_version only.
/// created_by and updated_by columns do NOT exist in the DB schema — both ignored in EF configuration.
/// </summary>
public class CaseNote : TenantEntity
{
    public Guid StudentId { get; set; }

    /// <summary>Free text or ref-driven note category (e.g. "behavioural", "medical", "general").</summary>
    public string? NoteType { get; set; }

    public string Body { get; set; } = string.Empty;

    /// <summary>When true, visible only to clinical staff with the appropriate permission.</summary>
    public bool IsConfidential { get; set; }

    /// <summary>FK to iam.user_account — the staff member who authored this note.</summary>
    public Guid? AuthoredBy { get; set; }
}
