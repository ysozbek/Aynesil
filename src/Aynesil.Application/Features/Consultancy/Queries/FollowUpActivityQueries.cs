using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetFollowUpActivitiesQuery ────────────────────────────────────────────────

public class GetFollowUpActivitiesQuery
    : PagedQuery, IRequest<PaginatedResult<FollowUpActivityListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? ConsultancyPlanId { get; set; }
    public Guid? SchoolVisitId { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? Status { get; set; }
    public DateOnly? DueBefore { get; set; }
    public DateOnly? DueAfter { get; set; }
}

public sealed class GetFollowUpActivitiesQueryHandler
    : IRequestHandler<GetFollowUpActivitiesQuery, PaginatedResult<FollowUpActivityListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetFollowUpActivitiesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<FollowUpActivityListItemDto>> Handle(
        GetFollowUpActivitiesQuery req, CancellationToken ct)
    {
        var q = _db.FollowUpActivities.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(a => a.CorporationId == req.CorporationId.Value);
        if (req.ConsultancyPlanId.HasValue)
            q = q.Where(a => a.ConsultancyPlanId == req.ConsultancyPlanId.Value);
        if (req.SchoolVisitId.HasValue)
            q = q.Where(a => a.SchoolVisitId == req.SchoolVisitId.Value);
        if (req.AssignedTo.HasValue)
            q = q.Where(a => a.AssignedTo == req.AssignedTo.Value);
        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(a => a.Status == req.Status);
        if (req.DueBefore.HasValue)
            q = q.Where(a => a.DueDate.HasValue && a.DueDate <= req.DueBefore.Value);
        if (req.DueAfter.HasValue)
            q = q.Where(a => a.DueDate.HasValue && a.DueDate >= req.DueAfter.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(a => a.Title.ToLower().Contains(term));
        }

        // Build base join query with anonymous type — sort BEFORE projection so EF Core
        // can translate OrderBy to SQL. Sorting on a projected DTO causes translation failure.
        var baseQ =
            from a in q
            join p in _db.ConsultancyPlans.AsNoTracking()
                on a.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            join v in _db.SchoolVisits.AsNoTracking()
                on a.SchoolVisitId equals v.Id into visitGrp
            from v in visitGrp.DefaultIfEmpty()
            select new { a, p, v };

        var sortedQ = req.SortBy?.ToLowerInvariant() switch
        {
            "title"   => req.IsDescending ? baseQ.OrderByDescending(x => x.a.Title)   : baseQ.OrderBy(x => x.a.Title),
            "duedate" => req.IsDescending ? baseQ.OrderByDescending(x => x.a.DueDate) : baseQ.OrderBy(x => x.a.DueDate),
            "status"  => req.IsDescending ? baseQ.OrderByDescending(x => x.a.Status)  : baseQ.OrderBy(x => x.a.Status),
            _         => baseQ.OrderBy(x => x.a.DueDate).ThenBy(x => x.a.CreatedAt)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new FollowUpActivityListItemDto(
                x.a.Id, x.a.CorporationId,
                x.a.ConsultancyPlanId, x.p != null ? x.p.Name : null,
                x.a.SchoolVisitId, x.v != null ? x.v.VisitDate : (DateOnly?)null,
                x.a.ObservationRecordId,
                x.a.Title, x.a.DueDate, x.a.AssignedTo,
                x.a.Status, x.a.CompletedAt, x.a.CreatedAt))
            .ToListAsync(ct);
        return PaginatedResult<FollowUpActivityListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetFollowUpActivityQuery ──────────────────────────────────────────────────

public record GetFollowUpActivityQuery(Guid Id) : IRequest<FollowUpActivityDto>;

public sealed class GetFollowUpActivityQueryHandler
    : IRequestHandler<GetFollowUpActivityQuery, FollowUpActivityDto>
{
    private readonly IAppDbContext _db;

    public GetFollowUpActivityQueryHandler(IAppDbContext db) => _db = db;

    public async Task<FollowUpActivityDto> Handle(
        GetFollowUpActivityQuery req, CancellationToken ct)
        => await CreateFollowUpActivityCommandHandler.ProjectFollowUpDto(_db, req.Id, ct)
           ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");
}

// ── GetOpenFollowUpsReportQuery ───────────────────────────────────────────────

/// <summary>
/// Open Follow-up Report: all pending/in_progress activities, highlighting overdue items.
/// </summary>
public class GetOpenFollowUpsReportQuery : IRequest<IReadOnlyList<OpenFollowUpReportItemDto>>
{
    public Guid CorporationId { get; set; }
    public Guid? ConsultancyPlanId { get; set; }
    public Guid? AssignedTo { get; set; }
    public bool OverdueOnly { get; set; }
}

public sealed class GetOpenFollowUpsReportQueryHandler
    : IRequestHandler<GetOpenFollowUpsReportQuery, IReadOnlyList<OpenFollowUpReportItemDto>>
{
    private readonly IAppDbContext _db;

    public GetOpenFollowUpsReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpenFollowUpReportItemDto>> Handle(
        GetOpenFollowUpsReportQuery req, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.FollowUpActivities.AsNoTracking()
            .Where(a => a.CorporationId == req.CorporationId
                     && (a.Status == "pending" || a.Status == "in_progress"));

        if (req.ConsultancyPlanId.HasValue)
            q = q.Where(a => a.ConsultancyPlanId == req.ConsultancyPlanId.Value);
        if (req.AssignedTo.HasValue)
            q = q.Where(a => a.AssignedTo == req.AssignedTo.Value);
        if (req.OverdueOnly)
            q = q.Where(a => a.DueDate.HasValue && a.DueDate < today);

        return await (
            from a in q
            join p in _db.ConsultancyPlans.AsNoTracking()
                on a.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            join v in _db.SchoolVisits.AsNoTracking()
                on a.SchoolVisitId equals v.Id into visitGrp
            from v in visitGrp.DefaultIfEmpty()
            select new OpenFollowUpReportItemDto(
                a.Id, a.Title,
                a.ConsultancyPlanId, p != null ? p.Name : null,
                a.SchoolVisitId, v != null ? v.VisitDate : (DateOnly?)null,
                a.DueDate,
                a.DueDate.HasValue && a.DueDate < today,
                a.AssignedTo, a.Status, a.CreatedAt)
        ).OrderBy(x => x.DueDate).ThenBy(x => x.CreatedAt).ToListAsync(ct);
    }
}
