using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Queries;

// ── GetAcademicPeriodsQuery ───────────────────────────────────────────────────

public class GetAcademicPeriodsQuery : PagedQuery, IRequest<PaginatedResult<AcademicPeriodListItemDto>>
{
    public bool? IsCurrent { get; set; }
}

public sealed class GetAcademicPeriodsQueryHandler
    : IRequestHandler<GetAcademicPeriodsQuery, PaginatedResult<AcademicPeriodListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAcademicPeriodsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<AcademicPeriodListItemDto>> Handle(
        GetAcademicPeriodsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var q = _db.AcademicPeriods.AsNoTracking();

        q = q.Where(p => p.CorporationId == corporationId);

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
            select new
            {
                p.Id,
                p.Name,
                p.TermId,
                TermLabel = term != null ? term.Code : null,
                p.StartDate,
                p.EndDate,
                p.IsCurrent
            };

        query = req.SortBy?.ToLower() switch
        {
            "name"      => req.IsDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "startdate" => req.IsDescending ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
            _           => query.OrderByDescending(p => p.StartDate)
        };

        var projected = query.Select(r =>
                new AcademicPeriodListItemDto(
                    r.Id,
                    r.Name,
                    r.TermId,
                    r.TermLabel,
                    r.StartDate,
                    r.EndDate,
                    r.IsCurrent));

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
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
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AcademicPeriod"));

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
