using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Students.Entities;

/// <summary>
/// Periodic development progress report authored by an educator or specialist.
/// Maps to students.development_report.
///
/// authored_by references an educator/staff user — no navigation property
/// to avoid cross-module coupling (educators module not yet implemented in C#).
///
/// Audit: created_at, created_by, updated_at, deleted_at, row_version.
/// updated_by column does NOT exist in the DB schema — ignored in EF configuration.
/// </summary>
public class DevelopmentReport : TenantEntity
{
    public Guid StudentId { get; set; }

    /// <summary>Human-readable period label, e.g. "2024-Q1", "September Term 2024".</summary>
    public string? PeriodLabel { get; set; }

    public DateOnly? ReportDate { get; set; }

    /// <summary>FK to iam.user_account (educator/specialist) — no navigation to avoid cross-module coupling.</summary>
    public Guid? AuthoredBy { get; set; }

    public string? Content { get; set; }

    /// <summary>FK to core.file_object — optional attached report document.</summary>
    public Guid? FileId { get; set; }

    /// <summary>
    /// Y15: guardians (parent portal) see finalized reports only. A report starts as a draft; once
    /// finalized it can no longer be edited (guardians may already have read it).
    /// </summary>
    public bool IsFinalized { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    /// <summary>iam.user_account id of the staff member who finalized the report.</summary>
    public Guid? FinalizedBy { get; private set; }

    public void Finalize(Guid? userId)
    {
        if (IsFinalized)
            throw new BusinessRuleViolationException(MsgKeys.Rule.DevelopmentReportAlreadyFinalized);

        IsFinalized = true;
        FinalizedAt = DateTimeOffset.UtcNow;
        FinalizedBy = userId;
        UpdatedAt   = FinalizedAt.Value;
    }

    /// <summary>Content/period/file may change only while the report is a draft.</summary>
    public void EnsureEditable()
    {
        if (IsFinalized)
            throw new BusinessRuleViolationException(MsgKeys.Rule.DevelopmentReportFinalized);
    }
}
