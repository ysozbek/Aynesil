using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// An immutable progress measurement for a student goal.
/// Recorded at a point in time; never updated (append-only series for trend analysis).
/// Maps to education.goal_progress.
///
/// Trend values are checked constraints in DDL: improving | stable | declining.
/// session_id is a soft reference to scheduling.session (added when Scheduling is built).
/// </summary>
public class GoalProgress : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid StudentGoalId { get; private set; }

    /// <summary>Soft reference to scheduling.session — null if recorded outside a session.</summary>
    public Guid? SessionId { get; private set; }

    public DateOnly MeasuredOn { get; private set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Raw numeric measurement (e.g. number of correct responses).</summary>
    public decimal? MeasuredValue { get; private set; }

    /// <summary>Percent of mastery achieved at this measurement point (0–100).</summary>
    public decimal? PercentComplete { get; private set; }

    /// <summary>improving | stable | declining. Checked constraint in DDL.</summary>
    public string? Trend { get; private set; }

    public string? Note { get; private set; }

    /// <summary>FK to iam.user_account — the educator who recorded this measurement.</summary>
    public Guid? RecordedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static GoalProgress Record(
        Guid corporationId,
        Guid studentGoalId,
        DateOnly measuredOn,
        decimal? measuredValue,
        decimal? percentComplete,
        string? trend,
        string? note,
        Guid? recordedBy,
        Guid? sessionId = null)
    {
        ValidateTrend(trend);

        var progress = new GoalProgress
        {
            CorporationId   = corporationId,
            StudentGoalId   = studentGoalId,
            SessionId       = sessionId,
            MeasuredOn      = measuredOn,
            MeasuredValue   = measuredValue,
            PercentComplete = percentComplete,
            Trend           = trend,
            Note            = note,
            RecordedBy      = recordedBy,
            CreatedAt       = DateTimeOffset.UtcNow
        };

        progress.AddDomainEvent(new GoalProgressRecordedEvent(
            progress.Id, corporationId, studentGoalId, measuredOn, percentComplete, trend));

        return progress;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidTrends = ["improving", "stable", "declining"];

    private static void ValidateTrend(string? trend)
    {
        if (trend is not null && !ValidTrends.Contains(trend))
            throw new ArgumentException(
                $"Invalid trend '{trend}'. Must be improving, stable, or declining.");
    }
}
