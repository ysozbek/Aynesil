using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Report received from an external institution (hospital, special education centre, etc.).
/// Maps to students.external_institution_report.
///
/// Audit: created_at, created_by, deleted_at, row_version only.
/// updated_at and updated_by columns do NOT exist in the DB schema — both ignored in EF configuration.
/// </summary>
public class ExternalInstitutionReport : TenantEntity
{
    public Guid StudentId { get; set; }

    public string InstitutionName { get; set; } = string.Empty;

    /// <summary>FK to ref.ref_value (ref_type: institution_type). Configurable. e.g. Hospital, Special Ed Centre.</summary>
    public Guid? InstitutionTypeId { get; set; }

    public DateOnly? ReportDate { get; set; }

    public string? Summary { get; set; }

    /// <summary>FK to core.file_object — the scanned or uploaded report.</summary>
    public Guid? FileId { get; set; }
}
