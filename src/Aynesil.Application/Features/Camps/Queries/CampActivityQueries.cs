using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Queries;

// ── GetCampActivitiesQuery ────────────────────────────────────────────────────

public class GetCampActivitiesQuery : PagedQuery, IRequest<PaginatedResult<CampActivityListItemDto>>
{
    public Guid? CampPeriodId { get; set; }
    public Guid? ActivityTypeId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetCampActivitiesQueryHandler
    : IRequestHandler<GetCampActivitiesQuery, PaginatedResult<CampActivityListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCampActivitiesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampActivityListItemDto>> Handle(
        GetCampActivitiesQuery req, CancellationToken ct)
    {
        var q = _db.CampActivities.AsNoTracking()
            .Where(a => a.DeletedAt == null);

        if (req.CampPeriodId.HasValue)
            q = q.Where(a => a.CampPeriodId == req.CampPeriodId.Value);

        if (req.ActivityTypeId.HasValue)
            q = q.Where(a => a.ActivityTypeId == req.ActivityTypeId.Value);

        if (req.IsActive.HasValue)
            q = q.Where(a => a.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(a => a.Name.ToLower().Contains(term));
        }

        var query =
            from a in q
            join typ in _db.RefValues.AsNoTracking()
                on a.ActivityTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new CampActivityListItemDto(
                a.Id,
                a.CampPeriodId,
                a.ActivityTypeId,
                typ != null ? typ.Code : null,
                a.Name,
                a.StartsAt,
                a.EndsAt,
                a.Location,
                a.Capacity,
                a.IsActive,
                a.Participations.Count);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "name"     => req.IsDescending ? query.OrderByDescending(x => x.Name)     : query.OrderBy(x => x.Name),
            "startsat" => req.IsDescending ? query.OrderByDescending(x => x.StartsAt) : query.OrderBy(x => x.StartsAt),
            _          => query.OrderBy(x => x.StartsAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<CampActivityListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetCampActivityQuery ──────────────────────────────────────────────────────

public record GetCampActivityQuery(Guid Id) : IRequest<CampActivityDto>;

public sealed class GetCampActivityQueryHandler : IRequestHandler<GetCampActivityQuery, CampActivityDto>
{
    private readonly IAppDbContext _db;

    public GetCampActivityQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CampActivityDto> Handle(GetCampActivityQuery req, CancellationToken ct)
    {
        var a = await _db.CampActivities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Camp activity {req.Id} not found.");

        var typeCode = a.ActivityTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == a.ActivityTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new CampActivityDto(
            a.Id, a.CorporationId, a.CampPeriodId,
            a.ActivityTypeId, typeCode,
            a.Name, a.Description, a.StartsAt, a.EndsAt,
            a.Location, a.Capacity, a.SessionId, a.IsActive,
            a.CreatedAt, a.UpdatedAt, a.RowVersion);
    }
}
