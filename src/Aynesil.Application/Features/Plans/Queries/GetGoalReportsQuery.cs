using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Application.Features.Plans.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Queries;

// ── GetStudentGoalSummaryReportQuery ──────────────────────────────────────────

/// <summary>
/// Student Goal Summary report: overall plan history + goal achievement for a student.
/// </summary>
public record GetStudentGoalSummaryReportQuery(
    Guid CorporationId,
    Guid StudentId) : IRequest<StudentGoalSummaryReportDto>;

public sealed class GetStudentGoalSummaryReportQueryHandler
    : IRequestHandler<GetStudentGoalSummaryReportQuery, StudentGoalSummaryReportDto>
{
    private readonly IAppDbContext _db;

    public GetStudentGoalSummaryReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentGoalSummaryReportDto> Handle(
        GetStudentGoalSummaryReportQuery req, CancellationToken ct)
    {
        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var plans = await (
            from p in _db.EducationPlans.AsNoTracking()
                .Where(p => p.CorporationId == req.CorporationId && p.StudentId == req.StudentId)
            join period in _db.AcademicPeriods.AsNoTracking()
                on p.AcademicPeriodId equals period.Id into periodGrp
            from period in periodGrp.DefaultIfEmpty()
            select new EducationPlanListItemDto(
                p.Id, p.StudentId, studentName,
                p.AcademicPeriodId, period != null ? period.Name : null,
                p.Title, p.Version, p.Status,
                p.EffectiveFrom, p.EffectiveTo,
                p.GuardianVisible, p.CreatedAt)
        ).ToListAsync(ct);

        var goals = await _db.StudentGoals.AsNoTracking()
            .Where(g => g.CorporationId == req.CorporationId && g.StudentId == req.StudentId)
            .ToListAsync(ct);

        var total    = goals.Count;
        var achieved = goals.Count(g => g.Status == "achieved");
        var rate     = total > 0 ? Math.Round((decimal)achieved / total * 100, 1) : 0m;

        return new StudentGoalSummaryReportDto(
            req.StudentId, studentName, plans, total, achieved, rate);
    }
}

// ── GetTrendReportQuery ───────────────────────────────────────────────────────

/// <summary>
/// Trend report: goal progress trends for all active goals in a student's current plan.
/// </summary>
public record GetTrendReportQuery(
    Guid CorporationId,
    Guid StudentId,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<IReadOnlyList<TrendReportRowDto>>;

public sealed class GetTrendReportQueryHandler
    : IRequestHandler<GetTrendReportQuery, IReadOnlyList<TrendReportRowDto>>
{
    private readonly IAppDbContext _db;

    public GetTrendReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrendReportRowDto>> Handle(
        GetTrendReportQuery req, CancellationToken ct)
    {
        var q = _db.StudentGoals.AsNoTracking()
            .Include(g => g.ProgressRecords)
            .Where(g => g.CorporationId == req.CorporationId
                     && g.StudentId == req.StudentId
                     && g.Status == "active");

        var goals = await q.ToListAsync(ct);

        return goals.Select(g =>
        {
            var series = g.ProgressRecords
                .Where(p => (req.From == null || p.MeasuredOn >= req.From)
                         && (req.To   == null || p.MeasuredOn <= req.To))
                .OrderBy(p => p.MeasuredOn)
                .ToList();

            var latest = series.LastOrDefault();

            return new TrendReportRowDto(
                g.Id, g.Statement, g.Horizon, g.TargetDate,
                latest?.PercentComplete,
                latest?.Trend,
                series.Count);
        })
        .OrderBy(r => r.Horizon)
        .ThenBy(r => r.Statement)
        .ToList();
    }
}
