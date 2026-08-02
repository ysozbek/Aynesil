using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

// ── GetEducatorDashboardQuery ─────────────────────────────────────────────────

/// <summary>
/// Educator self-service dashboard: current and previous period summaries,
/// all KPI values for the period, 6-month session-count and attendance trends,
/// and most recent 10 parent feedback entries.
/// Reads from pre-computed snapshots and kpi_value — no live aggregation.
/// </summary>
public record GetEducatorDashboardQuery(
    Guid CorporationId,
    Guid EducatorId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<EducatorDashboardDto>;

public sealed class GetEducatorDashboardQueryHandler
    : IRequestHandler<GetEducatorDashboardQuery, EducatorDashboardDto>
{
    private readonly IAppDbContext _db;

    public GetEducatorDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorDashboardDto> Handle(
        GetEducatorDashboardQuery req, CancellationToken ct)
    {
        var educator = await _db.Educators
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.EducatorId
                                   && e.CorporationId == req.CorporationId
                                   && e.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException(
                $"Educator {req.EducatorId} not found.");

        var fullName   = $"{educator.FirstName} {educator.LastName}";
        var titleCode  = educator.TitleId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == educator.TitleId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        // ── Current period snapshot ───────────────────────────────────────────
        var current = await _db.EducatorPerformanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EducatorId == req.EducatorId
                                   && s.PeriodStart == req.PeriodStart
                                   && s.PeriodEnd == req.PeriodEnd, ct);

        var periodDays   = req.PeriodEnd.DayNumber - req.PeriodStart.DayNumber + 1;
        var prevEnd      = req.PeriodStart.AddDays(-1);
        var prevStart    = prevEnd.AddDays(-periodDays + 1);

        var previous = await _db.EducatorPerformanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EducatorId == req.EducatorId
                                   && s.PeriodStart == prevStart
                                   && s.PeriodEnd == prevEnd, ct);

        // ── All KPI values for this period ────────────────────────────────────
        var kpiValues = await (
            from kv in _db.KpiValues.AsNoTracking()
            join kd in _db.KpiDefinitions.AsNoTracking() on kv.KpiId equals kd.Id
            where kv.CorporationId == req.CorporationId
               && kv.SubjectType == "educator"
               && kv.SubjectId == req.EducatorId
               && kv.PeriodStart == req.PeriodStart
               && kv.PeriodEnd == req.PeriodEnd
            orderby kd.Code
            select new KpiValueDto(
                kv.Id, kv.CorporationId, kv.KpiId,
                kd.Code, kd.Name, kd.Unit,
                kv.SubjectType, kv.SubjectId,
                kv.PeriodStart, kv.PeriodEnd,
                kv.NumericValue, kv.ComputedAt)
        ).ToListAsync(ct);

        // ── 6-period trend (session count + attendance) ───────────────────────
        var trendSnapshots = await _db.EducatorPerformanceSnapshots
            .AsNoTracking()
            .Where(s => s.EducatorId == req.EducatorId
                     && s.PeriodEnd <= req.PeriodEnd)
            .OrderByDescending(s => s.PeriodEnd)
            .Take(6)
            .ToListAsync(ct);

        trendSnapshots.Reverse();

        var sessionTrend = trendSnapshots
            .Select(s => new TrendPointDto(
                s.PeriodStart, s.PeriodEnd,
                FormatPeriodLabel(s.PeriodStart, s.PeriodEnd),
                s.SessionCount))
            .ToList();

        var attendanceTrend = trendSnapshots
            .Select(s => new TrendPointDto(
                s.PeriodStart, s.PeriodEnd,
                FormatPeriodLabel(s.PeriodStart, s.PeriodEnd),
                s.AttendanceRate))
            .ToList();

        // ── Recent parent feedback ─────────────────────────────────────────────
        var recentFeedback = await _db.ParentFeedbacks
            .AsNoTracking()
            .Where(pf => pf.EducatorId == req.EducatorId
                      && pf.CorporationId == req.CorporationId
                      && pf.Rating.HasValue)
            .OrderByDescending(pf => pf.CreatedAt)
            .Take(10)
            .Select(pf => new ParentFeedbackSummaryDto(
                pf.Id, pf.SessionId, pf.CreatedAt, pf.Rating!.Value, pf.Comment))
            .ToListAsync(ct);

        return new EducatorDashboardDto(
            educator.Id, fullName, titleCode,
            ToSummary(current, req.PeriodStart, req.PeriodEnd),
            ToSummary(previous, prevStart, prevEnd),
            kpiValues, sessionTrend, attendanceTrend, recentFeedback);
    }

    private static PerformanceSummaryDto? ToSummary(
        Aynesil.Domain.Modules.Ops.Entities.EducatorPerformanceSnapshot? s,
        DateOnly start, DateOnly end) =>
        s is null ? null : new PerformanceSummaryDto(
            s.PeriodStart, s.PeriodEnd, FormatPeriodLabel(start, end),
            s.SessionCount, s.AttendanceRate,
            s.GoalAchievementRate, s.ParentFeedbackAvg, s.UtilizationRate);

    private static string FormatPeriodLabel(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        return days <= 31
            ? start.ToString("MMM yyyy")
            : days <= 100
                ? $"Q{(start.Month - 1) / 3 + 1} {start.Year}"
                : start.Year.ToString();
    }
}

// ── GetManagerDashboardQuery ──────────────────────────────────────────────────

/// <summary>
/// Manager dashboard: aggregated corporation (or campus) performance for the period,
/// top performers (top 5 by attendance rate), and all educator summaries.
/// </summary>
public record GetManagerDashboardQuery(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? CampusId = null) : IRequest<ManagerDashboardDto>;

public sealed class GetManagerDashboardQueryHandler
    : IRequestHandler<GetManagerDashboardQuery, ManagerDashboardDto>
{
    private readonly IAppDbContext _db;

    public GetManagerDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ManagerDashboardDto> Handle(
        GetManagerDashboardQuery req, CancellationToken ct)
    {
        // Educators filtered optionally by campus
        var educatorIds = await (
            from e in _db.Educators.AsNoTracking()
            where e.CorporationId == req.CorporationId
               && e.IsActive && e.DeletedAt == null
            join ec in _db.EducatorCampuses.AsNoTracking()
                on e.Id equals ec.EducatorId into ecGrp
            from ec in ecGrp.DefaultIfEmpty()
            where req.CampusId == null || (ec != null && ec.CampusId == req.CampusId)
            select e.Id
        ).Distinct().ToListAsync(ct);

        // Snapshots for the period
        var snapshots = await _db.EducatorPerformanceSnapshots
            .AsNoTracking()
            .Where(s => s.CorporationId == req.CorporationId
                     && s.PeriodStart == req.PeriodStart
                     && s.PeriodEnd == req.PeriodEnd
                     && educatorIds.Contains(s.EducatorId))
            .ToListAsync(ct);

        // Educator names
        var educatorNames = await _db.Educators
            .AsNoTracking()
            .Where(e => educatorIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.TitleId, e.PrimaryCampusId })
            .ToDictionaryAsync(e => e.Id, ct);

        var summaries = snapshots
            .Select((s, i) =>
            {
                educatorNames.TryGetValue(s.EducatorId, out var ed);
                return new EducatorSummaryDto(
                    s.EducatorId,
                    ed is not null ? $"{ed.FirstName} {ed.LastName}" : s.EducatorId.ToString(),
                    null,
                    ed?.PrimaryCampusId,
                    s.SessionCount,
                    s.AttendanceRate,
                    s.GoalAchievementRate,
                    s.ParentFeedbackAvg,
                    s.UtilizationRate,
                    null);
            })
            .OrderByDescending(s => s.AttendanceRate)
            .ToList();

        // Rank educators by attendance
        var ranked = summaries
            .Select((s, idx) => s with { Rank = idx + 1 })
            .ToList();

        var top5 = ranked.Take(5).ToList();

        var avgAtt   = snapshots.Count > 0 ? (decimal?)snapshots.Average(s => s.AttendanceRate ?? 0) : null;
        var avgGoal  = snapshots.Count > 0 ? (decimal?)snapshots.Average(s => s.GoalAchievementRate ?? 0) : null;
        var avgFeed  = snapshots.Count > 0 ? (decimal?)snapshots.Average(s => s.ParentFeedbackAvg ?? 0) : null;
        var avgUtil  = snapshots.Count > 0 ? (decimal?)snapshots.Average(s => s.UtilizationRate ?? 0) : null;

        var label = FormatPeriodLabel(req.PeriodStart, req.PeriodEnd);

        return new ManagerDashboardDto(
            req.CorporationId, req.CampusId,
            req.PeriodStart, req.PeriodEnd, label,
            educatorIds.Count,
            snapshots.Count > 0 ? Math.Round(avgAtt!.Value, 2) : null,
            snapshots.Count > 0 ? Math.Round(avgGoal!.Value, 2) : null,
            snapshots.Count > 0 ? Math.Round(avgFeed!.Value, 2) : null,
            snapshots.Count > 0 ? Math.Round(avgUtil!.Value, 2) : null,
            top5, ranked);
    }

    private static string FormatPeriodLabel(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        return days <= 31
            ? start.ToString("MMM yyyy")
            : days <= 100
                ? $"Q{(start.Month - 1) / 3 + 1} {start.Year}"
                : start.Year.ToString();
    }
}

// ── GetExecutiveDashboardQuery ────────────────────────────────────────────────

/// <summary>
/// Executive dashboard: corporation-wide KPI aggregates with multi-period trends
/// (up to 12 most recent snapshots per KPI code), total session count, and top 10 performers.
/// </summary>
public record GetExecutiveDashboardQuery(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<ExecutiveDashboardDto>;

public sealed class GetExecutiveDashboardQueryHandler
    : IRequestHandler<GetExecutiveDashboardQuery, ExecutiveDashboardDto>
{
    private readonly IAppDbContext _db;

    public GetExecutiveDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ExecutiveDashboardDto> Handle(
        GetExecutiveDashboardQuery req, CancellationToken ct)
    {
        // Active educators
        var totalActiveEducators = await _db.Educators
            .AsNoTracking()
            .CountAsync(e => e.CorporationId == req.CorporationId
                          && e.IsActive && e.DeletedAt == null, ct);

        // Current period snapshots
        var currentSnapshots = await _db.EducatorPerformanceSnapshots
            .AsNoTracking()
            .Where(s => s.CorporationId == req.CorporationId
                     && s.PeriodStart == req.PeriodStart
                     && s.PeriodEnd == req.PeriodEnd)
            .ToListAsync(ct);

        var totalSessions = currentSnapshots.Sum(s => s.SessionCount ?? 0);
        var avgAtt  = currentSnapshots.Count > 0
            ? (decimal?)Math.Round(currentSnapshots.Average(s => s.AttendanceRate ?? 0), 2)
            : null;
        var avgGoal = currentSnapshots.Count > 0
            ? (decimal?)Math.Round(currentSnapshots.Average(s => s.GoalAchievementRate ?? 0), 2)
            : null;
        var avgFeed = currentSnapshots.Count > 0
            ? (decimal?)Math.Round(currentSnapshots.Average(s => s.ParentFeedbackAvg ?? 0), 2)
            : null;
        var avgUtil = currentSnapshots.Count > 0
            ? (decimal?)Math.Round(currentSnapshots.Average(s => s.UtilizationRate ?? 0), 2)
            : null;

        // ── Multi-period KPI trends (last 12 periods per metric) ──────────────
        var historicKpiValues = await (
            from kv in _db.KpiValues.AsNoTracking()
            join kd in _db.KpiDefinitions.AsNoTracking() on kv.KpiId equals kd.Id
            where kv.CorporationId == req.CorporationId
               && kv.SubjectType == "educator"
               && kv.PeriodEnd <= req.PeriodEnd
               && kd.Code.StartsWith("educator.")
               && kd.IsActive
            select new { kv.PeriodStart, kv.PeriodEnd, kv.NumericValue, kd.Code, kd.Name, kd.Unit }
        ).ToListAsync(ct);

        var trends = historicKpiValues
            .GroupBy(x => new { x.Code, x.Name, x.Unit })
            .Select(g =>
            {
                var points = g
                    .GroupBy(x => new { x.PeriodStart, x.PeriodEnd })
                    .OrderByDescending(p => p.Key.PeriodEnd)
                    .Take(12)
                    .Select(p => new TrendPointDto(
                        p.Key.PeriodStart, p.Key.PeriodEnd,
                        FormatPeriodLabel(p.Key.PeriodStart, p.Key.PeriodEnd),
                        p.Any(x => x.NumericValue.HasValue)
                            ? Math.Round(p.Where(x => x.NumericValue.HasValue).Average(x => x.NumericValue!.Value), 2)
                            : (decimal?)null))
                    .OrderBy(p => p.PeriodStart)
                    .ToList();

                return new KpiTrendDto(g.Key.Code, g.Key.Name, g.Key.Unit, points);
            })
            .ToList();

        // Top 10 performers by attendance rate
        var educatorNames = await _db.Educators
            .AsNoTracking()
            .Where(e => e.CorporationId == req.CorporationId && e.DeletedAt == null)
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.TitleId, e.PrimaryCampusId })
            .ToDictionaryAsync(e => e.Id, ct);

        var topPerformers = currentSnapshots
            .Where(s => s.AttendanceRate.HasValue)
            .OrderByDescending(s => s.AttendanceRate)
            .Take(10)
            .Select((s, idx) =>
            {
                educatorNames.TryGetValue(s.EducatorId, out var ed);
                return new EducatorSummaryDto(
                    s.EducatorId,
                    ed is not null ? $"{ed.FirstName} {ed.LastName}" : s.EducatorId.ToString(),
                    null, ed?.PrimaryCampusId,
                    s.SessionCount, s.AttendanceRate,
                    s.GoalAchievementRate, s.ParentFeedbackAvg, s.UtilizationRate,
                    idx + 1);
            })
            .ToList();

        return new ExecutiveDashboardDto(
            req.CorporationId,
            req.PeriodStart, req.PeriodEnd,
            totalActiveEducators, totalSessions,
            avgAtt, avgGoal, avgFeed, avgUtil,
            trends, topPerformers);
    }

    private static string FormatPeriodLabel(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        return days <= 31
            ? start.ToString("MMM yyyy")
            : days <= 100
                ? $"Q{(start.Month - 1) / 3 + 1} {start.Year}"
                : start.Year.ToString();
    }
}
