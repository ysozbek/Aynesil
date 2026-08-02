namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.educator_performance_snapshot.
/// Periodic performance rollup for one educator over a contiguous date range.
/// Used to feed the educator KPI dashboards and trend analysis.
///
/// This is a computed snapshot, NOT a transactional aggregate:
///   - Written / refreshed by ComputePerformanceSnapshotCommandHandler.
///   - Unique per (educator_id, period_start, period_end).
///   - Extended metrics (e.g. program_completion_rate, session_type breakdown) are
///     stored as JSON in the Detail column; the six named columns match the db DDL.
///
/// DB columns created_at / updated_at / created_by / updated_by / deleted_at /
/// row_version do not exist on this table — those inherited properties are ignored
/// in the EF Core configuration. ComputedAt replaces them for auditing purposes.
/// </summary>
public class EducatorPerformanceSnapshot : TenantEntity
{
    public Guid EducatorId { get; private set; }

    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>Count of sessions with status 'completed' where the educator was the lead.</summary>
    public int? SessionCount { get; private set; }

    /// <summary>
    /// Student attendance rate (present + late) / total_enrolled × 100
    /// across all completed sessions in the period.
    /// </summary>
    public decimal? AttendanceRate { get; private set; }

    /// <summary>
    /// Percentage of completed sessions in which at least one student goal was worked on.
    /// </summary>
    public decimal? GoalAchievementRate { get; private set; }

    /// <summary>Average parent feedback rating (1–5 scale) received in the period.</summary>
    public decimal? ParentFeedbackAvg { get; private set; }

    /// <summary>
    /// Completed sessions / (completed + cancelled + no_show) sessions × 100.
    /// Measures how productively the educator's scheduled time was used.
    /// </summary>
    public decimal? UtilizationRate { get; private set; }

    /// <summary>
    /// Supplementary breakdown data as JSON.
    /// Examples: program_completion_rate, session_count_by_type, total_attendance_minutes.
    /// </summary>
    public string Detail { get; private set; } = "{}";

    public DateTimeOffset ComputedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducatorPerformanceSnapshot Compute(
        Guid corporationId,
        Guid educatorId,
        DateOnly periodStart,
        DateOnly periodEnd,
        int? sessionCount,
        decimal? attendanceRate,
        decimal? goalAchievementRate,
        decimal? parentFeedbackAvg,
        decimal? utilizationRate,
        string? detail = null)
    {
        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start.");

        return new()
        {
            CorporationId       = corporationId,
            EducatorId          = educatorId,
            PeriodStart         = periodStart,
            PeriodEnd           = periodEnd,
            SessionCount        = sessionCount,
            AttendanceRate      = attendanceRate,
            GoalAchievementRate = goalAchievementRate,
            ParentFeedbackAvg   = parentFeedbackAvg,
            UtilizationRate     = utilizationRate,
            Detail              = detail ?? "{}",
            ComputedAt          = DateTimeOffset.UtcNow
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>Refreshes all metric values for an existing snapshot (upsert path).</summary>
    public void Refresh(
        int? sessionCount,
        decimal? attendanceRate,
        decimal? goalAchievementRate,
        decimal? parentFeedbackAvg,
        decimal? utilizationRate,
        string? detail = null)
    {
        SessionCount        = sessionCount;
        AttendanceRate      = attendanceRate;
        GoalAchievementRate = goalAchievementRate;
        ParentFeedbackAvg   = parentFeedbackAvg;
        UtilizationRate     = utilizationRate;
        Detail              = detail ?? "{}";
        ComputedAt          = DateTimeOffset.UtcNow;
    }
}
