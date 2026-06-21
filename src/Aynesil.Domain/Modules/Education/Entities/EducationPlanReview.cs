namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// A periodic review of an education plan's progress.
/// Immutable once created — append-only review ledger.
/// Outcome values are checked constraints in DDL: on_track | needs_revision | met.
/// Maps to education.education_plan_review.
/// </summary>
public class EducationPlanReview : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid EducationPlanId { get; private set; }

    public DateOnly ReviewedOn { get; private set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>FK to educators.educator — the educator conducting the review.</summary>
    public Guid? ReviewerId { get; private set; }

    public string? Summary { get; private set; }

    /// <summary>on_track | needs_revision | met. Checked constraint in DDL.</summary>
    public string? Outcome { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducationPlanReview Create(
        Guid corporationId,
        Guid educationPlanId,
        DateOnly reviewedOn,
        Guid? reviewerId,
        string? summary,
        string? outcome)
    {
        if (outcome is not null && !new[] { "on_track", "needs_revision", "met" }.Contains(outcome))
            throw new ArgumentException(
                $"Invalid review outcome '{outcome}'. Must be on_track, needs_revision, or met.");

        return new EducationPlanReview
        {
            CorporationId   = corporationId,
            EducationPlanId = educationPlanId,
            ReviewedOn      = reviewedOn,
            ReviewerId      = reviewerId,
            Summary         = summary,
            Outcome         = outcome,
            CreatedAt       = DateTimeOffset.UtcNow
        };
    }
}
