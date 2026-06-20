using Aynesil.Domain.Modules.Assessment.Events;

namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// Clinical / evaluation report produced from a completed assessment session.
/// Maps to assessment.assessment_report.
///
/// Reports progress through draft → finalized lifecycle.
/// Finalized reports are immutable — this is enforced at the domain level.
/// file_id references a core.file_object for the generated or uploaded report document.
///
/// DB columns present: id, corporation_id, assessment_session_id, summary, findings,
///   file_id, finalized_at, finalized_by, created_at, updated_at, row_version.
/// DB columns absent (no DB column): created_by, updated_by, deleted_at.
/// CreatedBy / UpdatedBy from AuditableEntity are ignored in EF configuration.
/// </summary>
public class AssessmentReport : AuditableEntity
{
    public Guid CorporationId { get; private set; }
    public Guid AssessmentSessionId { get; private set; }

    public string? Summary { get; private set; }
    public string? Findings { get; private set; }

    /// <summary>FK to core.file_object — uploaded or generated report document.</summary>
    public Guid? FileId { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    /// <summary>FK to iam.user_account — the professional who finalized the report.</summary>
    public Guid? FinalizedBy { get; private set; }

    public bool IsFinalized => FinalizedAt.HasValue;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentReport Create(
        Guid corporationId,
        Guid assessmentSessionId,
        string? summary = null,
        string? findings = null,
        Guid? fileId = null)
        => new()
        {
            CorporationId        = corporationId,
            AssessmentSessionId  = assessmentSessionId,
            Summary              = summary,
            Findings             = findings,
            FileId               = fileId,
            CreatedAt            = DateTimeOffset.UtcNow,
            UpdatedAt            = DateTimeOffset.UtcNow
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(
        string? summary,
        string? findings,
        Guid? fileId,
        Guid? updatedBy = null)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Finalized reports are immutable and cannot be edited.");

        Summary   = summary;
        Findings  = findings;
        FileId    = fileId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Locks the report. Once finalized the report text and file are immutable.
    /// Raises <see cref="AssessmentReportFinalizedEvent"/> for downstream consumers
    /// (e.g. notification to the assigning educator, outbox relay).
    /// </summary>
    public void Finalize(Guid finalizedBy)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Report is already finalized.");

        FinalizedAt = DateTimeOffset.UtcNow;
        FinalizedBy = finalizedBy;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = finalizedBy;

        AddDomainEvent(new AssessmentReportFinalizedEvent(
            Id, CorporationId, AssessmentSessionId, finalizedBy));
    }
}
