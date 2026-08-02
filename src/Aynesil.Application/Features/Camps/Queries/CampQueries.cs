using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampsQuery ─────────────────────────────────────────────────────────────

public class GetCampsQuery : PagedQuery, IRequest<PaginatedResult<CampListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? CampTypeId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetCampsQueryHandler
    : IRequestHandler<GetCampsQuery, PaginatedResult<CampListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCampsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampListItemDto>> Handle(
        GetCampsQuery req, CancellationToken ct)
    {
        var q = _db.Camps.AsNoTracking()
            .Where(c => c.DeletedAt == null);

        if (req.CorporationId.HasValue)
            q = q.Where(c => c.CorporationId == req.CorporationId.Value);

        if (req.CampusId.HasValue)
            q = q.Where(c => c.CampusId == req.CampusId.Value);

        if (req.CampTypeId.HasValue)
            q = q.Where(c => c.CampTypeId == req.CampTypeId.Value);

        if (req.IsActive.HasValue)
            q = q.Where(c => c.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(term)
                           || c.Code.ToLower().Contains(term));
        }

        var query =
            from c in q
            join typ in _db.RefValues.AsNoTracking()
                on c.CampTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new CampListItemDto(
                c.Id,
                c.CorporationId,
                c.CampusId,
                c.CampTypeId,
                typ != null ? typ.Code : null,
                c.Code,
                c.Name,
                c.Location,
                c.Capacity,
                c.IsActive,
                c.Periods.Count,
                c.UpdatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "name"   => req.IsDescending ? query.OrderByDescending(x => x.Name)   : query.OrderBy(x => x.Name),
            "code"   => req.IsDescending ? query.OrderByDescending(x => x.Code)   : query.OrderBy(x => x.Code),
            "active" => req.IsDescending ? query.OrderByDescending(x => x.IsActive): query.OrderBy(x => x.IsActive),
            _        => query.OrderBy(x => x.Name)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CampListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCampQuery ──────────────────────────────────────────────────────────────

public record GetCampQuery(Guid Id) : IRequest<CampDto>;

public sealed class GetCampQueryHandler : IRequestHandler<GetCampQuery, CampDto>
{
    private readonly IAppDbContext _db;

    public GetCampQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CampDto> Handle(GetCampQuery req, CancellationToken ct)
    {
        var camp = await _db.Camps.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp {req.Id} not found.");

        var typeCode = camp.CampTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == camp.CampTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        // Project periods with enrollment counts as DB-side subqueries (no N+1).
        var periods = await (
            from p in _db.CampPeriods.AsNoTracking()
            where p.CampId == req.Id
            orderby p.StartDate
            select new CampPeriodListItemDto(
                p.Id, p.CampId, p.Name, p.StartDate, p.EndDate, p.Capacity,
                p.Enrollments.Count(e => e.Status == "enrolled"),
                p.Enrollments.Count(e => e.Status == "waitlist"))
        ).ToListAsync(ct);

        return new CampDto(
            camp.Id, camp.CorporationId, camp.CampusId,
            camp.CampTypeId, typeCode,
            camp.Code, camp.Name, camp.Description, camp.Location,
            camp.Capacity, camp.IsActive,
            camp.CreatedAt, camp.UpdatedAt, camp.RowVersion,
            periods);
    }
}
