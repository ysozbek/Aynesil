using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Clinical diagnosis for a student. KVKK Art.6 special-category (health) data.
/// Maps to students.diagnosis.
///
/// Audit: created_at, created_by, updated_at, deleted_at, row_version.
/// updated_by column does NOT exist in the DB schema — ignored in EF configuration.
/// </summary>
public class Diagnosis : TenantEntity
{
    public Guid StudentId { get; set; }

    /// <summary>FK to ref.ref_value (ref_type: diagnosis_category). Configurable by business users.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>ICD-10/ICD-11 code, e.g. "F84.0".</summary>
    public string? IcdCode { get; set; }

    public string? Description { get; set; }

    public DateOnly? DiagnosedOn { get; set; }

    /// <summary>External clinician or institution name (free text — not an FK).</summary>
    public string? DiagnosedBy { get; set; }

    /// <summary>FK to core.file_object — supporting diagnostic document (report scan, etc.).</summary>
    public Guid? SourceFileId { get; set; }
}
