using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Queries;

// ── GetConsultancyPlansQuery ──────────────────────────────────────────────────

public class GetConsultancyPlansQuery
    : PagedQuery, IRequest<PaginatedResult<ConsultancyPlanListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string? Status { get; set; }
    public Guid? ConsultancyTypeId { get; set; }
    public Guid? LeadEducatorId { get; set; }
}

public sealed class GetConsultancyPlansQueryHandler
    : IRequestHandler<GetConsultancyPlansQuery, PaginatedResult<ConsultancyPlanListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetConsultancyPlansQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<ConsultancyPlanListItemDto>> Handle(
        GetConsultancyPlansQuery req, CancellationToken ct)
    {
        var q = _db.ConsultancyPlans.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(p => p.CorporationId == req.CorporationId.Value);
        if (req.InstitutionId.HasValue)
            q = q.Where(p => p.InstitutionId == req.InstitutionId.Value);
        if (!string.IsNullOrWhiteSpace(req.Status))
            q = q.Where(p => p.Status == req.Status);
        if (req.ConsultancyTypeId.HasValue)
            q = q.Where(p => p.ConsultancyTypeId == req.ConsultancyTypeId.Value);
        if (req.LeadEducatorId.HasValue)
            q = q.Where(p => p.LeadEducatorId == req.LeadEducatorId.Value);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(term));
        }

        var query =
            from p in q
            join i in _db.Institutions.AsNoTracking()
                on p.InstitutionId equals i.Id
            join typ in _db.RefValues.AsNoTracking()
                on p.ConsultancyTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ConsultancyPlanListItemDto(
                p.Id,
                p.CorporationId,
                p.InstitutionId,
                i.Name,
                p.ConsultancyTypeId,
                typ != null ? typ.Code : null,
                p.Name,
                p.PeriodStart,
                p.PeriodEnd,
                p.Status,
                p.Visits.Count,
                p.Reports.Count,
                p.CreatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "name"        => req.IsDescending ? query.OrderByDescending(x => x.Name)       : query.OrderBy(x => x.Name),
            "institution" => req.IsDescending ? query.OrderByDescending(x => x.InstitutionName) : query.OrderBy(x => x.InstitutionName),
            "status"      => req.IsDescending ? query.OrderByDescending(x => x.Status)     : query.OrderBy(x => x.Status),
            "periodstart" => req.IsDescending ? query.OrderByDescending(x => x.PeriodStart): query.OrderBy(x => x.PeriodStart),
            _             => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<ConsultancyPlanListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetConsultancyPlanQuery ───────────────────────────────────────────────────

public record GetConsultancyPlanQuery(Guid Id) : IRequest<ConsultancyPlanDto>;

public sealed class GetConsultancyPlanQueryHandler
    : IRequestHandler<GetConsultancyPlanQuery, ConsultancyPlanDto>
{
    private readonly IAppDbContext _db;

    public GetConsultancyPlanQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyPlanDto> Handle(
        GetConsultancyPlanQuery req, CancellationToken ct)
    {
        var plan = await (
            from p in _db.ConsultancyPlans.AsNoTracking()
            where p.Id == req.Id
            join i in _db.Institutions.AsNoTracking()
                on p.InstitutionId equals i.Id
            join typ in _db.RefValues.AsNoTracking()
                on p.ConsultancyTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ConsultancyPlanDto(
                p.Id, p.CorporationId, p.InstitutionId, i.Name,
                p.ConsultancyTypeId, typ != null ? typ.Code : null,
                p.Name, p.PeriodStart, p.PeriodEnd,
                p.Scope, p.LeadEducatorId, p.Status,
                p.CreatedAt, p.UpdatedAt, p.RowVersion)
        ).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.Id} not found.");

        return plan;
    }
}
