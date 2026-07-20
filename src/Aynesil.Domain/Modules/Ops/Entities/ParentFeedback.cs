namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.parent_feedback (existing DDL — not modified).
/// Simple, single-submission session feedback from a guardian: star rating + comment.
/// For multi-question configurable forms, use SurveyResponse + SurveyQuestionResponse instead.
/// Rating range: 1–5 (enforced by DB check constraint).
/// Inherits TenantEntity for corporation_id; unused audit columns are ignored in EF config.
/// </summary>
public class ParentFeedback : TenantEntity
{
    public Guid? GuardianId { get; private set; }
    public Guid? EducatorId { get; private set; }
    public Guid? SessionId { get; private set; }

    /// <summary>Star rating 1–5. Null if the guardian chose to leave only a comment.</summary>
    public short? Rating { get; private set; }

    public string? Comment { get; private set; }

    public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ParentFeedback Create(
        Guid corporationId,
        Guid? guardianId,
        Guid? educatorId,
        Guid? sessionId,
        short? rating,
        string? comment)
    {
        if (rating.HasValue && (rating < 1 || rating > 5))
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        return new()
        {
            CorporationId = corporationId,
            GuardianId    = guardianId,
            EducatorId    = educatorId,
            SessionId     = sessionId,
            Rating        = rating,
            Comment       = comment?.Trim(),
            CreatedAt     = DateTimeOffset.UtcNow
        };
    }
}
