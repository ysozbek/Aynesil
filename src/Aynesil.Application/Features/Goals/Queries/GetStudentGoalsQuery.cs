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

    public GetStudentGoalsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<StudentGoalListItemDto>> Handle(
        GetStudentGoalsQuery req, CancellationToken ct)
    {
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

        var query =
            from g in q
            join cat in _db.RefValues.AsNoTracking()
                on g.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            join dev in _db.RefValues.AsNoTracking()
                on g.DevelopmentAreaId equals dev.Id into devGrp
            from dev in devGrp.DefaultIfEmpty()
            let latestProgress = g.ProgressRecords
                .Where(p => p.StudentGoalId == g.Id)
                .OrderByDescending(p => p.MeasuredOn)
                .FirstOrDefault()
            select new StudentGoalListItemDto(
                g.Id, g.StudentId,
                g.CategoryId, cat != null ? cat.Code : null,
                g.DevelopmentAreaId, dev != null ? dev.Code : null,
                g.Horizon, g.Statement, g.Status,
                g.TargetDate, g.AchievedDate,
                latestProgress != null ? latestProgress.PercentComplete : null,
                latestProgress != null ? latestProgress.Trend : null,
                g.CreatedAt);

        query = req.SortBy?.ToLower() switch
        {
            "status"    => req.IsDescending ? query.OrderByDescending(g => g.Status) : query.OrderBy(g => g.Status),
            "targetdate"=> req.IsDescending ? query.OrderByDescending(g => g.TargetDate) : query.OrderBy(g => g.TargetDate),
            "createdat" => req.IsDescending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
            _           => query.OrderBy(g => g.Horizon).ThenBy(g => g.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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
