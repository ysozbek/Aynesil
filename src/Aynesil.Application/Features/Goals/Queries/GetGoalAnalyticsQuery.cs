using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Queries;

// ── GetStudentGoalSummaryQuery ────────────────────────────────────────────────

/// <summary>
/// Returns a per-student goal summary: counts by status and achievement rate,
/// broken down by development area. Used for Student Progress reports.
/// </summary>
public record GetStudentGoalSummaryQuery(
    Guid CorporationId,
    Guid StudentId) : IRequest<StudentGoalSummaryDto>;

public sealed class GetStudentGoalSummaryQueryHandler
    : IRequestHandler<GetStudentGoalSummaryQuery, StudentGoalSummaryDto>
{
    private readonly IAppDbContext _db;

    public GetStudentGoalSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentGoalSummaryDto> Handle(
        GetStudentGoalSummaryQuery req, CancellationToken ct)
    {
        var goals = await _db.StudentGoals
            .AsNoTracking()
            .Where(g => g.CorporationId == req.CorporationId && g.StudentId == req.StudentId)
            .ToListAsync(ct);

        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == req.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var total       = goals.Count;
        var active      = goals.Count(g => g.Status == "active");
        var achieved    = goals.Count(g => g.Status == "achieved");
        var discontinued = goals.Count(g => g.Status == "discontinued");
        var onHold      = goals.Count(g => g.Status == "on_hold");
        var rate        = total > 0 ? Math.Round((decimal)achieved / total * 100, 1) : 0m;

        var devAreaGroups = goals
            .GroupBy(g => g.DevelopmentAreaId)
            .Select(grp => new { DevelopmentAreaId = grp.Key, Goals = grp.ToList() })
            .ToList();

        var byDevArea = new List<DevelopmentAreaProgressDto>();
        foreach (var grp in devAreaGroups)
        {
            var devAreaLabel = grp.DevelopmentAreaId.HasValue
                ? await _db.RefValues.AsNoTracking()
                    .Where(r => r.Id == grp.DevelopmentAreaId.Value)
                    .Select(r => r.Code).FirstOrDefaultAsync(ct)
                : null;

            var achievedInArea = grp.Goals.Count(g => g.Status == "achieved");
            var areaRate = grp.Goals.Count > 0
                ? Math.Round((decimal)achievedInArea / grp.Goals.Count * 100, 1)
                : 0m;

            byDevArea.Add(new DevelopmentAreaProgressDto(
                grp.DevelopmentAreaId, devAreaLabel, grp.Goals.Count, achievedInArea, areaRate));
        }

        return new StudentGoalSummaryDto(
            req.StudentId, studentName,
            total, active, achieved, discontinued, onHold, rate,
            byDevArea);
    }
}

// ── GetGoalSuccessRatesQuery ──────────────────────────────────────────────────

/// <summary>
/// Returns goal success rates grouped by goal category for a corporation.
/// Used for Goal Success reports and Therapy Progress reports.
/// </summary>
public record GetGoalSuccessRatesQuery(
    Guid CorporationId,
    Guid? CampusId = null,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<IReadOnlyList<GoalSuccessRateDto>>;

public sealed class GetGoalSuccessRatesQueryHandler
    : IRequestHandler<GetGoalSuccessRatesQuery, IReadOnlyList<GoalSuccessRateDto>>
{
    private readonly IAppDbContext _db;

    public GetGoalSuccessRatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<GoalSuccessRateDto>> Handle(
        GetGoalSuccessRatesQuery req, CancellationToken ct)
    {
        var q = _db.StudentGoals.AsNoTracking()
            .Where(g => g.CorporationId == req.CorporationId);

        if (req.From.HasValue)
            q = q.Where(g => g.StartDate == null || g.StartDate >= req.From.Value);

        if (req.To.HasValue)
            q = q.Where(g => g.StartDate == null || g.StartDate <= req.To.Value);

        var grouped = await q
            .GroupBy(g => g.CategoryId)
            .Select(grp => new
            {
                CategoryId = grp.Key,
                Total      = grp.Count(),
                Achieved   = grp.Count(g => g.Status == "achieved")
            })
            .ToListAsync(ct);

        var result = new List<GoalSuccessRateDto>();
        foreach (var grp in grouped)
        {
            var catLabel = grp.CategoryId.HasValue
                ? await _db.RefValues.AsNoTracking()
                    .Where(r => r.Id == grp.CategoryId.Value)
                    .Select(r => r.Code).FirstOrDefaultAsync(ct)
                : null;

            var successRate = grp.Total > 0
                ? Math.Round((decimal)grp.Achieved / grp.Total * 100, 1)
                : 0m;

            result.Add(new GoalSuccessRateDto(
                grp.CategoryId, catLabel, grp.Total, grp.Achieved, successRate, null));
        }

        return result.OrderByDescending(r => r.SuccessRate).ToList();
    }
}
