using Aynesil.Application.Common.CareTeam;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Queries;

// ── GetStudentGoalsQuery ──────────────────────────────────────────────────────

public class GetStudentGoalsQuery : PagedQuery, IRequest<PaginatedResult<StudentGoalListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? StudentId { get; set; }
    public string? Horizon { get; set; }
    public string? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? DevelopmentAreaId { get; set; }
}

public sealed class GetStudentGoalsQueryHandler
    : IRequestHandler<GetStudentGoalsQuery, PaginatedResult<StudentGoalListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetStudentGoalsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<StudentGoalListItemDto>> Handle(
        GetStudentGoalsQuery req, CancellationToken ct)
    {
        // When filtering by a specific student, apply care-team pre-filter.
        if (req.StudentId.HasValue &&
            !CareTeamFilter.HasBypass(_currentUser) &&
            !await CareTeamFilter.CanAccessStudentAsync(_db, _currentUser, req.StudentId.Value, ct))
            return PaginatedResult<StudentGoalListItemDto>.Create([], 0, req.Page, req.PageSize);

        var q = _db.StudentGoals.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(g => g.CorporationId == req.CorporationId.Value);
        if (req.StudentId.HasValue)
            q = q.Where(g => g.StudentId == req.StudentId.Value);
        if (req.Horizon is not null)
            q = q.Where(g => g.Horizon == req.Horizon);
        if (req.Status is not null)
            q = q.Where(g => g.Status == req.Status);
        if (req.CategoryId.HasValue)
            q = q.Where(g => g.CategoryId == req.CategoryId.Value);
        if (req.DevelopmentAreaId.HasValue)
            q = q.Where(g => g.DevelopmentAreaId == req.DevelopmentAreaId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(g => g.Statement.ToLower().Contains(s));
        }

        // Sort on entity/join before DTO projection — EF cannot translate OrderBy on DTO ctor.
        var joined =
            from g in q
            join cat in _db.RefValues.AsNoTracking()
                on g.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            join dev in _db.RefValues.AsNoTracking()
                on g.DevelopmentAreaId equals dev.Id into devGrp
            from dev in devGrp.DefaultIfEmpty()
            select new { g, cat, dev };

        var sorted = req.SortBy?.ToLower() switch
        {
            "status"     => req.IsDescending ? joined.OrderByDescending(x => x.g.Status)     : joined.OrderBy(x => x.g.Status),
            "targetdate" => req.IsDescending ? joined.OrderByDescending(x => x.g.TargetDate) : joined.OrderBy(x => x.g.TargetDate),
            "createdat"  => req.IsDescending ? joined.OrderByDescending(x => x.g.CreatedAt)  : joined.OrderBy(x => x.g.CreatedAt),
            _            => joined.OrderBy(x => x.g.Horizon).ThenBy(x => x.g.CreatedAt)
        };

        var total = await sorted.CountAsync(ct);
        var items = await sorted
            .Skip(req.Skip)
            .Take(req.PageSize)
            .Select(x => new StudentGoalListItemDto(
                x.g.Id, x.g.StudentId,
                x.g.CategoryId, x.cat != null ? x.cat.Code : null,
                x.g.DevelopmentAreaId, x.dev != null ? x.dev.Code : null,
                x.g.Horizon, x.g.Statement, x.g.Status,
                x.g.TargetDate, x.g.AchievedDate,
                x.g.ProgressRecords
                    .Where(p => p.StudentGoalId == x.g.Id)
                    .OrderByDescending(p => p.MeasuredOn)
                    .Select(p => (decimal?)p.PercentComplete)
                    .FirstOrDefault(),
                x.g.ProgressRecords
                    .Where(p => p.StudentGoalId == x.g.Id)
                    .OrderByDescending(p => p.MeasuredOn)
                    .Select(p => p.Trend)
                    .FirstOrDefault(),
                x.g.CreatedAt))
            .ToListAsync(ct);
        return PaginatedResult<StudentGoalListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetStudentGoalQuery ───────────────────────────────────────────────────────

public record GetStudentGoalQuery(Guid Id) : IRequest<StudentGoalDto>;

public sealed class GetStudentGoalQueryHandler : IRequestHandler<GetStudentGoalQuery, StudentGoalDto>
{
    private readonly IAppDbContext _db;

    public GetStudentGoalQueryHandler(IAppDbContext db) => _db = db;

    public async Task<StudentGoalDto> Handle(GetStudentGoalQuery req, CancellationToken ct)
        => await GoalProjection.LoadAsync(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"StudentGoal {req.Id} not found.");
}
