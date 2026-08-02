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

        var query =
            from i in q
            join typ in _db.RefValues.AsNoTracking()
                on i.InstitutionTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new InstitutionListItemDto(
                i.Id,
                i.CorporationId,
                i.InstitutionTypeId,
                typ != null ? typ.Code : null,
                i.Name,
                i.City,
                i.District,
                i.Plans.Count(p => p.Status != "cancelled"),
                i.Visits.Count,
                i.CreatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "name"     => req.IsDescending ? query.OrderByDescending(x => x.Name)   : query.OrderBy(x => x.Name),
            "city"     => req.IsDescending ? query.OrderByDescending(x => x.City)   : query.OrderBy(x => x.City),
            "createdat"=> req.IsDescending ? query.OrderByDescending(x => x.CreatedAt): query.OrderBy(x => x.CreatedAt),
            _          => query.OrderBy(x => x.Name)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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
