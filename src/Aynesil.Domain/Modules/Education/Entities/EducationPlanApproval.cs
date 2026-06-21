namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// An approval decision record for an education plan.
/// Immutable once created — the decision ledger is append-only.
/// Decision values are checked constraints in DDL: approved | rejected | changes_requested.
/// Maps to education.education_plan_approval.
/// </summary>
public class EducationPlanApproval : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid EducationPlanId { get; private set; }

    /// <summary>FK to educators.educator — the coordinator making the decision.</summary>
    public Guid? ApproverId { get; private set; }

    /// <summary>approved | rejected | changes_requested. Checked constraint in DDL.</summary>
    public string Decision { get; private set; } = string.Empty;

    public string? Comment { get; private set; }

    public DateTimeOffset DecidedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducationPlanApproval Create(
        Guid corporationId,
        Guid educationPlanId,
        Guid? approverId,
        string decision,
        string? comment = null)
    {
        if (!new[] { "approved", "rejected", "changes_requested" }.Contains(decision))
            throw new ArgumentException(
                $"Invalid decision '{decision}'. Must be approved, rejected, or changes_requested.");

        return new EducationPlanApproval
        {
            CorporationId   = corporationId,
            EducationPlanId = educationPlanId,
            ApproverId      = approverId,
            Decision        = decision,
            Comment         = comment,
            DecidedAt       = DateTimeOffset.UtcNow
        };
    }
}
