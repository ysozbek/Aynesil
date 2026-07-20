using Aynesil.Application.Common.CareTeam;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Queries;

// ── GetGoalProgressQuery ──────────────────────────────────────────────────────

/// <summary>Returns all progress measurements for a specific student goal, ordered by date.</summary>
public record GetGoalProgressQuery(
    Guid StudentGoalId,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<IReadOnlyList<GoalProgressDto>>;

public sealed class GetGoalProgressQueryHandler
    : IRequestHandler<GetGoalProgressQuery, IReadOnlyList<GoalProgressDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetGoalProgressQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GoalProgressDto>> Handle(
        GetGoalProgressQuery req, CancellationToken ct)
    {
        // Resolve the parent student_goal to get the student_id for care-team check.
        // If the goal is not visible (RLS hides it), this returns null → empty list.
        if (!CareTeamFilter.HasBypass(_currentUser))
        {
            var studentId = await _db.StudentGoals.AsNoTracking()
                .Where(g => g.Id == req.StudentGoalId)
                .Select(g => (Guid?)g.StudentId)
                .FirstOrDefaultAsync(ct);

            if (!studentId.HasValue ||
                !await CareTeamFilter.CanAccessStudentAsync(_db, _currentUser, studentId.Value, ct))
                return [];
        }

        var q = _db.GoalProgressRecords.AsNoTracking()
            .Where(p => p.StudentGoalId == req.StudentGoalId);

        if (req.From.HasValue)
            q = q.Where(p => p.MeasuredOn >= req.From.Value);

        if (req.To.HasValue)
            q = q.Where(p => p.MeasuredOn <= req.To.Value);

        return await q
            .OrderBy(p => p.MeasuredOn)
            .Select(p => new GoalProgressDto(
                p.Id, p.StudentGoalId, p.SessionId, p.MeasuredOn,
                p.MeasuredValue, p.PercentComplete, p.Trend, p.Note,
                p.RecordedBy, p.CreatedAt))
            .ToListAsync(ct);
    }
}

// ── GetGoalTrendQuery ─────────────────────────────────────────────────────────

/// <summary>Returns progress trend analysis for a single student goal.</summary>
public record GetGoalTrendQuery(Guid StudentGoalId) : IRequest<GoalTrendDto>;

public sealed class GetGoalTrendQueryHandler : IRequestHandler<GetGoalTrendQuery, GoalTrendDto>
{
    private readonly IAppDbContext _db;

    public GetGoalTrendQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GoalTrendDto> Handle(GetGoalTrendQuery req, CancellationToken ct)
    {
        var goal = await _db.StudentGoals
            .AsNoTracking()
            .Include(g => g.ProgressRecords)
            .FirstOrDefaultAsync(g => g.Id == req.StudentGoalId, ct)
            ?? throw new KeyNotFoundException($"StudentGoal {req.StudentGoalId} not found.");

        var series = goal.ProgressRecords
            .OrderBy(p => p.MeasuredOn)
            .Select(GoalProjection.ToProgressDto)
            .ToList();

        var latest = series.LastOrDefault();

        return new GoalTrendDto(
            goal.Id,
            goal.Statement,
            goal.Horizon,
            goal.Status,
            series,
            latest?.PercentComplete,
            latest?.Trend);
    }
}
