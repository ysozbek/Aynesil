using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Queries;

// ── GetAcademicPeriodsQuery ───────────────────────────────────────────────────

public class GetAcademicPeriodsQuery : PagedQuery, IRequest<PaginatedResult<AcademicPeriodListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public bool? IsCurrent { get; set; }
}

public sealed class GetAcademicPeriodsQueryHandler
    : IRequestHandler<GetAcademicPeriodsQuery, PaginatedResult<AcademicPeriodListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetAcademicPeriodsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<AcademicPeriodListItemDto>> Handle(
        GetAcademicPeriodsQuery req, CancellationToken ct)
    {
        var q = _db.AcademicPeriods.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(p => p.CorporationId == req.CorporationId.Value);

        if (req.IsCurrent.HasValue)
            q = q.Where(p => p.IsCurrent == req.IsCurrent.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s));
        }

        var query =
            from p in q
            join term in _db.RefValues.AsNoTracking()
                on p.TermId equals term.Id into termGrp
            from term in termGrp.DefaultIfEmpty()
            select new AcademicPeriodListItemDto(
                p.Id, p.Name, p.TermId,
                term != null ? term.Code : null,
                p.StartDate, p.EndDate, p.IsCurrent);

        query = req.SortBy?.ToLower() switch
        {
            "name"      => req.IsDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "startdate" => req.IsDescending ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
            _           => query.OrderByDescending(p => p.StartDate)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<AcademicPeriodListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetAcademicPeriodQuery ────────────────────────────────────────────────────

public record GetAcademicPeriodQuery(Guid Id) : IRequest<AcademicPeriodDto>;

public sealed class GetAcademicPeriodQueryHandler
    : IRequestHandler<GetAcademicPeriodQuery, AcademicPeriodDto>
{
    private readonly IAppDbContext _db;

    public GetAcademicPeriodQueryHandler(IAppDbContext db) => _db = db;

    public async Task<AcademicPeriodDto> Handle(GetAcademicPeriodQuery req, CancellationToken ct)
    {
        var p = await _db.AcademicPeriods.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"AcademicPeriod {req.Id} not found.");

        var termLabel = p.TermId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == p.TermId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new AcademicPeriodDto(
            p.Id, p.CorporationId, p.Name, p.TermId, termLabel,
            p.StartDate, p.EndDate, p.IsCurrent,
            p.CreatedAt, p.UpdatedAt, p.RowVersion);
    }
}
