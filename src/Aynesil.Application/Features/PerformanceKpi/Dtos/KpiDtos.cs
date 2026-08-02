namespace Aynesil.Application.Features.PerformanceKpi.Dtos;

// ── KPI Category ──────────────────────────────────────────────────────────────

public record KpiCategoryDto(Guid Id, string Code, string? Label);

// ── KPI Definition ────────────────────────────────────────────────────────────

public record KpiDefinitionListItemDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string Name,
    Guid? CategoryId,
    string? CategoryCode,
    string? Unit,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public record KpiDefinitionDto(
    Guid Id,
    Guid? CorporationId,
    string Code,
    string Name,
    Guid? CategoryId,
    string? CategoryCode,
    string? Unit,
    string Spec,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── KPI Value ─────────────────────────────────────────────────────────────────

public record KpiValueDto(
    Guid Id,
    Guid CorporationId,
    Guid KpiId,
    string KpiCode,
    string KpiName,
    string? KpiUnit,
    string SubjectType,
    Guid? SubjectId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal? NumericValue,
    DateTimeOffset ComputedAt);

// ── Educator Performance Snapshot ─────────────────────────────────────────────

public record EducatorPerformanceSnapshotListItemDto(
    Guid Id,
    Guid EducatorId,
    string EducatorFullName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int? SessionCount,
    decimal? AttendanceRate,
    decimal? GoalAchievementRate,
    decimal? ParentFeedbackAvg,
    decimal? UtilizationRate,
    DateTimeOffset ComputedAt);

public record EducatorPerformanceSnapshotDto(
    Guid Id,
    Guid CorporationId,
    Guid EducatorId,
    string EducatorFullName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int? SessionCount,
    decimal? AttendanceRate,
    decimal? GoalAchievementRate,
    decimal? ParentFeedbackAvg,
    decimal? UtilizationRate,
    string Detail,
    DateTimeOffset ComputedAt);

// ── Parent Feedback ───────────────────────────────────────────────────────────

public record ParentFeedbackDto(
    Guid Id,
    Guid CorporationId,
    Guid? GuardianId,
    Guid? EducatorId,
    Guid? SessionId,
    short? Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

public record ParentFeedbackSummaryDto(
    Guid Id,
    Guid? SessionId,
    DateTimeOffset CreatedAt,
    short Rating,
    string? Comment);

// ── Dashboard building blocks ─────────────────────────────────────────────────

public record PerformanceSummaryDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeriodLabel,
    int? SessionCount,
    decimal? AttendanceRate,
    decimal? GoalAchievementRate,
    decimal? ParentFeedbackAvg,
    decimal? UtilizationRate);

public record TrendPointDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Label,
    decimal? Value);

public record KpiTrendDto(
    string KpiCode,
    string KpiName,
    string? Unit,
    IReadOnlyList<TrendPointDto> Points);

public record EducatorSummaryDto(
    Guid EducatorId,
    string FullName,
    string? TitleCode,
    Guid? PrimaryCampusId,
    int? SessionCount,
    decimal? AttendanceRate,
    decimal? GoalAchievementRate,
    decimal? ParentFeedbackAvg,
    decimal? UtilizationRate,
    int? Rank);

public record RankingItemDto(
    int Rank,
    Guid EducatorId,
    string FullName,
    string? TitleCode,
    decimal? KpiValue,
    string KpiCode,
    string KpiName,
    string? Unit);

// ── Educator Dashboard ────────────────────────────────────────────────────────

public record EducatorDashboardDto(
    Guid EducatorId,
    string FullName,
    string? TitleCode,
    PerformanceSummaryDto? CurrentPeriod,
    PerformanceSummaryDto? PreviousPeriod,
    IReadOnlyList<KpiValueDto> AllKpiValues,
    IReadOnlyList<TrendPointDto> SessionCountTrend,
    IReadOnlyList<TrendPointDto> AttendanceRateTrend,
    IReadOnlyList<ParentFeedbackSummaryDto> RecentFeedback);

// ── Manager Dashboard ─────────────────────────────────────────────────────────

public record ManagerDashboardDto(
    Guid CorporationId,
    Guid? CampusId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeriodLabel,
    int TotalEducators,
    decimal? AvgAttendanceRate,
    decimal? AvgGoalAchievementRate,
    decimal? AvgParentSatisfaction,
    decimal? AvgUtilizationRate,
    IReadOnlyList<EducatorSummaryDto> TopPerformers,
    IReadOnlyList<EducatorSummaryDto> Educators);

// ── Executive Dashboard ───────────────────────────────────────────────────────

public record ExecutiveDashboardDto(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int TotalActiveEducators,
    int TotalCompletedSessions,
    decimal? CorpAvgAttendanceRate,
    decimal? CorpAvgGoalAchievementRate,
    decimal? CorpAvgParentSatisfaction,
    decimal? CorpAvgUtilizationRate,
    IReadOnlyList<KpiTrendDto> Trends,
    IReadOnlyList<EducatorSummaryDto> TopPerformers);

// ── Report DTOs ───────────────────────────────────────────────────────────────

public record KpiReportRowDto(
    Guid EducatorId,
    string FullName,
    string? TitleCode,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int? SessionCount,
    decimal? AttendanceRate,
    decimal? GoalAchievementRate,
    decimal? ParentFeedbackAvg,
    decimal? UtilizationRate,
    int? Rank);
