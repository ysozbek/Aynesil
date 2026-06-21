using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Dtos;

// ── Academic Period DTOs ──────────────────────────────────────────────────────

public record AcademicPeriodDto(
    Guid Id,
    Guid CorporationId,
    string Name,
    Guid? TermId,
    string? TermLabel,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

public record AcademicPeriodListItemDto(
    Guid Id,
    string Name,
    Guid? TermId,
    string? TermLabel,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent);

// ── Education Plan DTOs ───────────────────────────────────────────────────────

public record EducationPlanDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string StudentName,
    Guid? AcademicPeriodId,
    string? AcademicPeriodName,
    Guid? CampusId,
    string? CampusName,
    string Title,
    int Version,
    string Status,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid? PreparedBy,
    string? PreparedByName,
    Guid? ApprovedBy,
    string? ApprovedByName,
    DateTimeOffset? ApprovedAt,
    bool GuardianVisible,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<EducationPlanGoalDto> LongTermGoals,
    IReadOnlyList<EducationPlanGoalDto> ShortTermGoals,
    IReadOnlyList<EducationPlanReviewDto> Reviews,
    IReadOnlyList<EducationPlanApprovalDto> Approvals,
    IReadOnlyList<EducationPlanRevisionDto> Revisions);

public record EducationPlanListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid? AcademicPeriodId,
    string? AcademicPeriodName,
    string Title,
    int Version,
    string Status,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    bool GuardianVisible,
    DateTimeOffset CreatedAt);

public record EducationPlanGoalDto(
    Guid Id,
    Guid StudentGoalId,
    string Statement,
    string Horizon,
    string GoalStatus,
    Guid? CategoryId,
    string? CategoryLabel,
    DateOnly? TargetDate,
    DateOnly? AchievedDate,
    int SortOrder,
    decimal? LatestPercentComplete,
    string? LatestTrend);

public record EducationPlanReviewDto(
    Guid Id,
    DateOnly ReviewedOn,
    Guid? ReviewerId,
    string? ReviewerName,
    string? Summary,
    string? Outcome,
    DateTimeOffset CreatedAt);

public record EducationPlanApprovalDto(
    Guid Id,
    Guid? ApproverId,
    string? ApproverName,
    string Decision,
    string? Comment,
    DateTimeOffset DecidedAt);

public record EducationPlanRevisionDto(
    Guid Id,
    int FromVersion,
    int ToVersion,
    string? ChangeSummary,
    Guid? RevisedBy,
    string? RevisedByName,
    DateTimeOffset RevisedAt);

// ── Report DTOs ───────────────────────────────────────────────────────────────

public record StudentGoalSummaryReportDto(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<EducationPlanListItemDto> Plans,
    int TotalGoals,
    int AchievedGoals,
    decimal AchievementRate);

public record TrendReportRowDto(
    Guid StudentGoalId,
    string Statement,
    string Horizon,
    DateOnly? TargetDate,
    decimal? LatestPercentComplete,
    string? CurrentTrend,
    int MeasurementCount);

// ── Projection Helper ─────────────────────────────────────────────────────────

internal static class PlanProjection
{
    public static async Task<EducationPlanDto?> LoadAsync(
        IAppDbContext db, Guid planId, CancellationToken ct)
    {
        var plan = await db.EducationPlans
            .AsNoTracking()
            .Include(p => p.PlanGoals)
                .ThenInclude(pg => pg.Goal)
                    .ThenInclude(g => g.ProgressRecords)
            .Include(p => p.Reviews)
            .Include(p => p.Approvals)
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return null;

        var studentName = await db.Students.AsNoTracking()
            .Where(s => s.Id == plan.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var campusName = plan.CampusId.HasValue
            ? await db.Campuses.AsNoTracking()
                .Where(c => c.Id == plan.CampusId.Value).Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        var periodName = plan.AcademicPeriodId.HasValue
            ? await db.AcademicPeriods.AsNoTracking()
                .Where(p => p.Id == plan.AcademicPeriodId.Value).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : null;

        var preparedByName = plan.PreparedBy.HasValue
            ? await db.Educators.AsNoTracking()
                .Where(e => e.Id == plan.PreparedBy.Value)
                .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct)
            : null;

        var approvedByName = plan.ApprovedBy.HasValue
            ? await db.Educators.AsNoTracking()
                .Where(e => e.Id == plan.ApprovedBy.Value)
                .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct)
            : null;

        var longTermGoals  = await BuildPlanGoalDtosAsync(db, plan.PlanGoals, "long_term",  ct);
        var shortTermGoals = await BuildPlanGoalDtosAsync(db, plan.PlanGoals, "short_term", ct);
        var reviews        = await BuildReviewDtosAsync(db, plan.Reviews, ct);
        var approvals      = await BuildApprovalDtosAsync(db, plan.Approvals, ct);
        var revisions      = await BuildRevisionDtosAsync(db, plan.Revisions, ct);

        return new EducationPlanDto(
            plan.Id, plan.CorporationId,
            plan.StudentId, studentName,
            plan.AcademicPeriodId, periodName,
            plan.CampusId, campusName,
            plan.Title, plan.Version, plan.Status,
            plan.EffectiveFrom, plan.EffectiveTo,
            plan.PreparedBy, preparedByName,
            plan.ApprovedBy, approvedByName,
            plan.ApprovedAt, plan.GuardianVisible,
            plan.CreatedAt, plan.UpdatedAt, plan.RowVersion,
            longTermGoals, shortTermGoals, reviews, approvals, revisions);
    }

    private static async Task<IReadOnlyList<EducationPlanGoalDto>> BuildPlanGoalDtosAsync(
        IAppDbContext db,
        IEnumerable<EducationPlanGoal> planGoals,
        string horizon,
        CancellationToken ct)
    {
        var filtered = planGoals.Where(pg => pg.Horizon == horizon).OrderBy(pg => pg.SortOrder);
        var result = new List<EducationPlanGoalDto>();

        foreach (var pg in filtered)
        {
            var goal = pg.Goal;
            if (goal is null) continue;

            var catLabel = goal.CategoryId.HasValue
                ? await db.RefValues.AsNoTracking()
                    .Where(r => r.Id == goal.CategoryId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
                : null;

            var latestProgress = goal.ProgressRecords
                .OrderByDescending(p => p.MeasuredOn)
                .FirstOrDefault();

            result.Add(new EducationPlanGoalDto(
                pg.Id, pg.StudentGoalId,
                goal.Statement, pg.Horizon, goal.Status,
                goal.CategoryId, catLabel,
                goal.TargetDate, goal.AchievedDate,
                pg.SortOrder,
                latestProgress?.PercentComplete,
                latestProgress?.Trend));
        }

        return result;
    }

    private static async Task<IReadOnlyList<EducationPlanReviewDto>> BuildReviewDtosAsync(
        IAppDbContext db, IEnumerable<EducationPlanReview> reviews, CancellationToken ct)
    {
        var result = new List<EducationPlanReviewDto>();
        foreach (var r in reviews.OrderByDescending(x => x.ReviewedOn))
        {
            var reviewerName = r.ReviewerId.HasValue
                ? await db.Educators.AsNoTracking()
                    .Where(e => e.Id == r.ReviewerId.Value)
                    .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct)
                : null;

            result.Add(new EducationPlanReviewDto(
                r.Id, r.ReviewedOn, r.ReviewerId, reviewerName,
                r.Summary, r.Outcome, r.CreatedAt));
        }
        return result;
    }

    private static async Task<IReadOnlyList<EducationPlanApprovalDto>> BuildApprovalDtosAsync(
        IAppDbContext db, IEnumerable<EducationPlanApproval> approvals, CancellationToken ct)
    {
        var result = new List<EducationPlanApprovalDto>();
        foreach (var a in approvals.OrderByDescending(x => x.DecidedAt))
        {
            var approverName = a.ApproverId.HasValue
                ? await db.Educators.AsNoTracking()
                    .Where(e => e.Id == a.ApproverId.Value)
                    .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct)
                : null;

            result.Add(new EducationPlanApprovalDto(
                a.Id, a.ApproverId, approverName,
                a.Decision, a.Comment, a.DecidedAt));
        }
        return result;
    }

    private static async Task<IReadOnlyList<EducationPlanRevisionDto>> BuildRevisionDtosAsync(
        IAppDbContext db, IEnumerable<EducationPlanRevision> revisions, CancellationToken ct)
    {
        var result = new List<EducationPlanRevisionDto>();
        foreach (var rev in revisions.OrderByDescending(x => x.RevisedAt))
        {
            var revisedByName = rev.RevisedBy.HasValue
                ? await db.Educators.AsNoTracking()
                    .Where(e => e.Id == rev.RevisedBy.Value)
                    .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct)
                : null;

            result.Add(new EducationPlanRevisionDto(
                rev.Id, rev.FromVersion, rev.ToVersion,
                rev.ChangeSummary, rev.RevisedBy, revisedByName, rev.RevisedAt));
        }
        return result;
    }
}
