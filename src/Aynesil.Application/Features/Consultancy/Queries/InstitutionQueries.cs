using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetInstitutionsQuery ──────────────────────────────────────────────────────

public class GetInstitutionsQuery : PagedQuery, IRequest<PaginatedResult<InstitutionListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? InstitutionTypeId { get; set; }
    public string? City { get; set; }
}

public sealed class GetInstitutionsQueryHandler
    : IRequestHandler<GetInstitutionsQuery, PaginatedResult<InstitutionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetInstitutionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<InstitutionListItemDto>> Handle(
        GetInstitutionsQuery req, CancellationToken ct)
    {
        var q = _db.Institutions.AsNoTracking()
            .Where(i => i.DeletedAt == null);

        if (req.CorporationId.HasValue)
            q = q.Where(i => i.CorporationId == req.CorporationId.Value);
        if (req.InstitutionTypeId.HasValue)
            q = q.Where(i => i.InstitutionTypeId == req.InstitutionTypeId.Value);
        if (!string.IsNullOrWhiteSpace(req.City))
            q = q.Where(i => i.City != null && i.City.ToLower().Contains(req.City.ToLower()));
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(i => i.Name.ToLower().Contains(term)
                           || (i.District != null && i.District.ToLower().Contains(term)));
        }

        var baseQ =
            from i in q
            join typ in _db.RefValues.AsNoTracking()
                on i.InstitutionTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new { i, typ };

        var sortedQ = req.SortBy?.ToLowerInvariant() switch
        {
            "name"     => req.IsDescending ? baseQ.OrderByDescending(x => x.i.Name)      : baseQ.OrderBy(x => x.i.Name),
            "city"     => req.IsDescending ? baseQ.OrderByDescending(x => x.i.City)      : baseQ.OrderBy(x => x.i.City),
            "createdat"=> req.IsDescending ? baseQ.OrderByDescending(x => x.i.CreatedAt) : baseQ.OrderBy(x => x.i.CreatedAt),
            _          => baseQ.OrderBy(x => x.i.Name)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new InstitutionListItemDto(
                x.i.Id, x.i.CorporationId,
                x.i.InstitutionTypeId, x.typ != null ? x.typ.Code : null,
                x.i.Name, x.i.City, x.i.District,
                x.i.Plans.Count(p => p.Status != "cancelled"),
                x.i.Visits.Count,
                x.i.CreatedAt))
            .ToListAsync(ct);
        return PaginatedResult<InstitutionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetInstitutionQuery ───────────────────────────────────────────────────────

public record GetInstitutionQuery(Guid Id) : IRequest<InstitutionDto>;

public sealed class GetInstitutionQueryHandler : IRequestHandler<GetInstitutionQuery, InstitutionDto>
{
    private readonly IAppDbContext _db;

    public GetInstitutionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<InstitutionDto> Handle(GetInstitutionQuery req, CancellationToken ct)
    {
        var institution = await (
            from i in _db.Institutions.AsNoTracking()
            where i.Id == req.Id && i.DeletedAt == null
            join typ in _db.RefValues.AsNoTracking()
                on i.InstitutionTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new InstitutionDto(
                i.Id, i.CorporationId,
                i.InstitutionTypeId, typ != null ? typ.Code : null,
                i.Name, i.City, i.District,
                i.ContactName, i.ContactPhone, i.ContactEmail,
                i.CreatedAt, i.UpdatedAt, i.RowVersion)
        ).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Institution {req.Id} not found.");

        return institution;
    }
}
