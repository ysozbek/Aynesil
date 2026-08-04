using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

// ── GetPerformanceSnapshotsQuery ──────────────────────────────────────────────

/// <summary>
/// Paginated list of performance snapshots. Supports filtering by educator,
/// campus, period range, and free-text search on educator name.
/// Primary use: Educator Performance Report screen.
/// </summary>
public class GetPerformanceSnapshotsQuery : PagedQuery,
    IRequest<PaginatedResult<EducatorPerformanceSnapshotListItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? EducatorId { get; set; }
    public Guid? CampusId { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
}

public sealed class GetPerformanceSnapshotsQueryHandler
    : IRequestHandler<GetPerformanceSnapshotsQuery,
        PaginatedResult<EducatorPerformanceSnapshotListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetPerformanceSnapshotsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<EducatorPerformanceSnapshotListItemDto>> Handle(
        GetPerformanceSnapshotsQuery req, CancellationToken ct)
    {
        var q =
            from s in _db.EducatorPerformanceSnapshots.AsNoTracking()
            join e in _db.Educators.AsNoTracking() on s.EducatorId equals e.Id
            where s.CorporationId == req.CorporationId
               && e.DeletedAt == null
            select new { s, e };

        if (req.EducatorId.HasValue)
            q = q.Where(x => x.s.EducatorId == req.EducatorId.Value);

        if (req.PeriodStart.HasValue)
            q = q.Where(x => x.s.PeriodStart >= req.PeriodStart.Value);

        if (req.PeriodEnd.HasValue)
            q = q.Where(x => x.s.PeriodEnd <= req.PeriodEnd.Value);

        if (req.CampusId.HasValue)
        {
            var campusIds = _db.EducatorCampuses
                .AsNoTracking()
                .Where(ec => ec.CampusId == req.CampusId.Value)
                .Select(ec => ec.EducatorId);
            q = q.Where(x => campusIds.Contains(x.s.EducatorId));
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(x =>
                (x.e.FirstName + " " + x.e.LastName).ToLower().Contains(term));
        }

        // Sort BEFORE projection on entity properties to allow EF Core SQL translation.
        var sortedQ = req.SortBy?.ToLowerInvariant() switch
        {
            "name"        => req.IsDescending ? q.OrderByDescending(x => x.e.LastName).ThenByDescending(x => x.e.FirstName) : q.OrderBy(x => x.e.LastName).ThenBy(x => x.e.FirstName),
            "period"      => req.IsDescending ? q.OrderByDescending(x => x.s.PeriodStart) : q.OrderBy(x => x.s.PeriodStart),
            "sessions"    => req.IsDescending ? q.OrderByDescending(x => x.s.SessionCount) : q.OrderBy(x => x.s.SessionCount),
            "attendance"  => req.IsDescending ? q.OrderByDescending(x => x.s.AttendanceRate) : q.OrderBy(x => x.s.AttendanceRate),
            "goal"        => req.IsDescending ? q.OrderByDescending(x => x.s.GoalAchievementRate) : q.OrderBy(x => x.s.GoalAchievementRate),
            "feedback"    => req.IsDescending ? q.OrderByDescending(x => x.s.ParentFeedbackAvg) : q.OrderBy(x => x.s.ParentFeedbackAvg),
            "utilization" => req.IsDescending ? q.OrderByDescending(x => x.s.UtilizationRate) : q.OrderBy(x => x.s.UtilizationRate),
            _             => q.OrderByDescending(x => x.s.PeriodEnd).ThenBy(x => x.e.LastName)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new EducatorPerformanceSnapshotListItemDto(
                x.s.Id, x.s.EducatorId,
                x.e.FirstName + " " + x.e.LastName,
                x.s.PeriodStart, x.s.PeriodEnd,
                x.s.SessionCount, x.s.AttendanceRate,
                x.s.GoalAchievementRate, x.s.ParentFeedbackAvg,
                x.s.UtilizationRate, x.s.ComputedAt))
            .ToListAsync(ct);
        return PaginatedResult<EducatorPerformanceSnapshotListItemDto>.Create(
            items, total, req.Page, req.PageSize);
    }
}

// ── GetKpiReportQuery ─────────────────────────────────────────────────────────

/// <summary>
/// KPI Report: all educators' snapshot metrics for a specific period,
/// with ordinal rank by attendance rate. Used for formal performance review exports.
/// </summary>
public record GetKpiReportQuery(
    Guid CorporationId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? CampusId = null) : IRequest<IReadOnlyList<KpiReportRowDto>>;

public sealed class GetKpiReportQueryHandler
    : IRequestHandler<GetKpiReportQuery, IReadOnlyList<KpiReportRowDto>>
{
    private readonly IAppDbContext _db;

    public GetKpiReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KpiReportRowDto>> Handle(
        GetKpiReportQuery req, CancellationToken ct)
    {
        var q =
            from s in _db.EducatorPerformanceSnapshots.AsNoTracking()
            join e in _db.Educators.AsNoTracking() on s.EducatorId equals e.Id
            where s.CorporationId == req.CorporationId
               && s.PeriodStart == req.PeriodStart
               && s.PeriodEnd == req.PeriodEnd
               && e.DeletedAt == null
            select new { s, e };

        if (req.CampusId.HasValue)
        {
            var campusIds = _db.EducatorCampuses
                .AsNoTracking()
                .Where(ec => ec.CampusId == req.CampusId.Value)
                .Select(ec => ec.EducatorId);
            q = q.Where(x => campusIds.Contains(x.s.EducatorId));
        }

        var rows = await q
            .OrderByDescending(x => x.s.AttendanceRate)
            .Select(x => new
            {
                x.s.EducatorId,
                FullName            = x.e.FirstName + " " + x.e.LastName,
                TitleId             = (Guid?)x.e.TitleId,
                x.s.PeriodStart,
                x.s.PeriodEnd,
                x.s.SessionCount,
                x.s.AttendanceRate,
                x.s.GoalAchievementRate,
                x.s.ParentFeedbackAvg,
                x.s.UtilizationRate
            })
            .ToListAsync(ct);

        return rows
            .Select((r, idx) => new KpiReportRowDto(
                r.EducatorId, r.FullName, null,
                r.PeriodStart, r.PeriodEnd,
                r.SessionCount, r.AttendanceRate,
                r.GoalAchievementRate, r.ParentFeedbackAvg,
                r.UtilizationRate, idx + 1))
            .ToList();
    }
}

// ── GetParentFeedbackQuery ─────────────────────────────────────────────────────

/// <summary>
/// Paginated parent feedback list. Filterable by educator, session, guardian, rating.
/// </summary>
public class GetParentFeedbackQuery : PagedQuery, IRequest<PaginatedResult<ParentFeedbackDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? EducatorId { get; set; }
    public Guid? GuardianId { get; set; }
    public Guid? SessionId { get; set; }
    public short? MinRating { get; set; }
    public short? MaxRating { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}

public sealed class GetParentFeedbackQueryHandler
    : IRequestHandler<GetParentFeedbackQuery, PaginatedResult<ParentFeedbackDto>>
{
    private readonly IAppDbContext _db;

    public GetParentFeedbackQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ParentFeedbackDto>> Handle(
        GetParentFeedbackQuery req, CancellationToken ct)
    {
        var q = _db.ParentFeedbacks.AsNoTracking()
            .Where(pf => pf.CorporationId == req.CorporationId);

        if (req.EducatorId.HasValue) q = q.Where(pf => pf.EducatorId == req.EducatorId.Value);
        if (req.GuardianId.HasValue) q = q.Where(pf => pf.GuardianId == req.GuardianId.Value);
        if (req.SessionId.HasValue)  q = q.Where(pf => pf.SessionId  == req.SessionId.Value);
        if (req.MinRating.HasValue)  q = q.Where(pf => pf.Rating >= req.MinRating.Value);
        if (req.MaxRating.HasValue)  q = q.Where(pf => pf.Rating <= req.MaxRating.Value);
        if (req.FromDate.HasValue)   q = q.Where(pf => pf.CreatedAt >= req.FromDate.Value);
        if (req.ToDate.HasValue)     q = q.Where(pf => pf.CreatedAt <= req.ToDate.Value);

        var projection = q.Select(pf => new ParentFeedbackDto(
            pf.Id, pf.CorporationId,
            pf.GuardianId, pf.EducatorId, pf.SessionId,
            pf.Rating, pf.Comment, pf.CreatedAt));

        projection = req.SortBy?.ToLowerInvariant() switch
        {
            "rating" => req.IsDescending ? projection.OrderByDescending(x => x.Rating) : projection.OrderBy(x => x.Rating),
            "date"   => req.IsDescending ? projection.OrderByDescending(x => x.CreatedAt) : projection.OrderBy(x => x.CreatedAt),
            _        => projection.OrderByDescending(x => x.CreatedAt)
        };

        var total = await projection.CountAsync(ct);
        var items = await projection.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ParentFeedbackDto>.Create(items, total, req.Page, req.PageSize);
    }
}
