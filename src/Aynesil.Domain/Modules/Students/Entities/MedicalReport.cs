using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Medical report filed for a student (hospital letters, therapy reports, etc.).
/// Maps to students.medical_report.
///
/// Audit: created_at, created_by, updated_at, deleted_at, row_version.
/// updated_by column does NOT exist in the DB schema — ignored in EF configuration.
/// </summary>
public class MedicalReport : TenantEntity
{
    public Guid StudentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly? ReportDate { get; set; }

    /// <summary>Issuing hospital, clinic, or clinician name (free text).</summary>
    public string? Issuer { get; set; }

    public string? Summary { get; set; }

    /// <summary>FK to core.file_object — the scanned or uploaded report document.</summary>
    public Guid? FileId { get; set; }
}
