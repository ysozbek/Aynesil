using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Dtos;

// ── Goal Library DTOs ─────────────────────────────────────────────────────────

public record GoalLibraryDto(
    Guid Id,
    Guid? CorporationId,
    string Name,
    string? Description,
    int TemplateCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record GoalLibraryListItemDto(
    Guid Id,
    Guid? CorporationId,
    string Name,
    string? Description,
    int TemplateCount,
    DateTimeOffset CreatedAt);

// ── Goal Template DTOs ────────────────────────────────────────────────────────

public record GoalTemplateDto(
    Guid Id,
    Guid? CorporationId,
    Guid? LibraryId,
    string? LibraryName,
    Guid? CategoryId,
    string? CategoryLabel,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    string? Code,
    string Statement,
    string? DefaultCriteria,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<GoalTemplateTranslationDto> Translations);

public record GoalTemplateListItemDto(
    Guid Id,
    Guid? CorporationId,
    Guid? LibraryId,
    string? LibraryName,
    Guid? CategoryId,
    string? CategoryLabel,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    string? Code,
    string Statement,
    DateTimeOffset CreatedAt);

public record GoalTemplateTranslationDto(
    string Locale,
    string Statement,
    string? DefaultCriteria);

// ── Student Goal DTOs ─────────────────────────────────────────────────────────

public record StudentGoalDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    Guid? TemplateId,
    Guid? CategoryId,
    string? CategoryLabel,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    string Horizon,
    Guid? ParentGoalId,
    string Statement,
    string? MasteryCriteria,
    string? Baseline,
    decimal? TargetValue,
    string Status,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    DateOnly? AchievedDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<GoalProgressDto> RecentProgress);

public record StudentGoalListItemDto(
    Guid Id,
    Guid StudentId,
    Guid? CategoryId,
    string? CategoryLabel,
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    string Horizon,
    string Statement,
    string Status,
    DateOnly? TargetDate,
    DateOnly? AchievedDate,
    decimal? LatestPercentComplete,
    string? LatestTrend,
    DateTimeOffset CreatedAt);

// ── Goal Progress DTOs ────────────────────────────────────────────────────────

public record GoalProgressDto(
    Guid Id,
    Guid StudentGoalId,
    Guid? SessionId,
    DateOnly MeasuredOn,
    decimal? MeasuredValue,
    decimal? PercentComplete,
    string? Trend,
    string? Note,
    Guid? RecordedBy,
    DateTimeOffset CreatedAt);

// ── Analytics DTOs ────────────────────────────────────────────────────────────

public record GoalTrendDto(
    Guid StudentGoalId,
    string Statement,
    string Horizon,
    string Status,
    IReadOnlyList<GoalProgressDto> ProgressSeries,
    decimal? LatestPercentComplete,
    string? CurrentTrend);

public record StudentGoalSummaryDto(
    Guid StudentId,
    string StudentName,
    int TotalGoals,
    int ActiveGoals,
    int AchievedGoals,
    int DiscontinuedGoals,
    int OnHoldGoals,
    decimal AchievementRate,
    IReadOnlyList<DevelopmentAreaProgressDto> ByDevelopmentArea);

public record DevelopmentAreaProgressDto(
    Guid? DevelopmentAreaId,
    string? DevelopmentAreaLabel,
    int GoalCount,
    int AchievedCount,
    decimal AchievementRate);

public record GoalSuccessRateDto(
    Guid? CategoryId,
    string? CategoryLabel,
    int TotalGoals,
    int AchievedGoals,
    decimal SuccessRate,
    string? AverageTrend);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class GoalProjection
{
    public static async Task<StudentGoalDto?> LoadAsync(
        IAppDbContext db, Guid goalId, CancellationToken ct)
    {
        var goal = await db.StudentGoals
            .AsNoTracking()
            .Include(g => g.ProgressRecords)
            .FirstOrDefaultAsync(g => g.Id == goalId, ct);

        if (goal is null) return null;

        var categoryLabel = goal.CategoryId.HasValue
            ? await db.RefValues.AsNoTracking()
                .Where(r => r.Id == goal.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var devAreaLabel = goal.DevelopmentAreaId.HasValue
            ? await db.RefValues.AsNoTracking()
                .Where(r => r.Id == goal.DevelopmentAreaId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var recentProgress = goal.ProgressRecords
            .OrderByDescending(p => p.MeasuredOn)
            .Take(10)
            .Select(ToProgressDto)
            .ToList();

        return new StudentGoalDto(
            goal.Id, goal.CorporationId, goal.StudentId, goal.TemplateId,
            goal.CategoryId, categoryLabel,
            goal.DevelopmentAreaId, devAreaLabel,
            goal.Horizon, goal.ParentGoalId,
            goal.Statement, goal.MasteryCriteria, goal.Baseline, goal.TargetValue,
            goal.Status, goal.StartDate, goal.TargetDate, goal.AchievedDate,
            goal.CreatedAt, goal.UpdatedAt, goal.RowVersion,
            recentProgress);
    }

    public static GoalTemplateTranslationDto ToTranslationDto(GoalTemplateTranslation t)
        => new(t.Locale, t.Statement, t.DefaultCriteria);

    public static GoalProgressDto ToProgressDto(GoalProgress p)
        => new(p.Id, p.StudentGoalId, p.SessionId, p.MeasuredOn,
               p.MeasuredValue, p.PercentComplete, p.Trend, p.Note,
               p.RecordedBy, p.CreatedAt);
}
