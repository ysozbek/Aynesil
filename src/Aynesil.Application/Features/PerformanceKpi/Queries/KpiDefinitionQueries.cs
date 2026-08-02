using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.PerformanceKpi.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

// ── GetKpiDefinitionsQuery ────────────────────────────────────────────────────

public class GetKpiDefinitionsQuery : PagedQuery, IRequest<PaginatedResult<KpiDefinitionListItemDto>>
{
    /// <summary>Filter by tenant. NULL returns platform-level KPIs only.</summary>
    public Guid? CorporationId { get; set; }

    /// <summary>When true, includes platform-level KPIs (corporation_id IS NULL) alongside tenant ones.</summary>
    public bool IncludePlatform { get; set; } = true;

    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class GetKpiDefinitionsQueryHandler
    : IRequestHandler<GetKpiDefinitionsQuery, PaginatedResult<KpiDefinitionListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetKpiDefinitionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<KpiDefinitionListItemDto>> Handle(
        GetKpiDefinitionsQuery req, CancellationToken ct)
    {
        var q = _db.KpiDefinitions.AsNoTracking();

        if (req.CorporationId.HasValue)
        {
            q = req.IncludePlatform
                ? q.Where(k => k.CorporationId == req.CorporationId || k.CorporationId == null)
                : q.Where(k => k.CorporationId == req.CorporationId);
        }

        if (req.CategoryId.HasValue)
            q = q.Where(k => k.CategoryId == req.CategoryId.Value);

        if (req.IsActive.HasValue)
            q = q.Where(k => k.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLower();
            q = q.Where(k => k.Name.ToLower().Contains(term)
                           || k.Code.ToLower().Contains(term));
        }

        var query =
            from k in q
            join cat in _db.RefValues.AsNoTracking()
                on k.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            select new KpiDefinitionListItemDto(
                k.Id, k.CorporationId, k.Code, k.Name,
                k.CategoryId, cat != null ? cat.Code : null,
                k.Unit, k.IsActive, k.UpdatedAt);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "name"   => req.IsDescending ? query.OrderByDescending(x => x.Name)   : query.OrderBy(x => x.Name),
            "code"   => req.IsDescending ? query.OrderByDescending(x => x.Code)   : query.OrderBy(x => x.Code),
            "active" => req.IsDescending ? query.OrderByDescending(x => x.IsActive): query.OrderBy(x => x.IsActive),
            _        => query.OrderBy(x => x.Code)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<KpiDefinitionListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetKpiDefinitionQuery ─────────────────────────────────────────────────────

public record GetKpiDefinitionQuery(Guid Id) : IRequest<KpiDefinitionDto>;

public sealed class GetKpiDefinitionQueryHandler
    : IRequestHandler<GetKpiDefinitionQuery, KpiDefinitionDto>
{
    private readonly IAppDbContext _db;

    public GetKpiDefinitionQueryHandler(IAppDbContext db) => _db = db;

    public async Task<KpiDefinitionDto> Handle(
        GetKpiDefinitionQuery req, CancellationToken ct)
    {
        var kpi = await _db.KpiDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"KPI definition {req.Id} not found.");

        var categoryCode = kpi.CategoryId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == kpi.CategoryId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new KpiDefinitionDto(
            kpi.Id, kpi.CorporationId, kpi.Code, kpi.Name,
            kpi.CategoryId, categoryCode, kpi.Unit, kpi.Spec,
            kpi.IsActive, kpi.CreatedAt, kpi.UpdatedAt, kpi.RowVersion);
    }
}

// ── GetKpiCategoriesQuery ─────────────────────────────────────────────────────

public record GetKpiCategoriesQuery(Guid? CorporationId) : IRequest<IReadOnlyList<KpiCategoryDto>>;

public sealed class GetKpiCategoriesQueryHandler
    : IRequestHandler<GetKpiCategoriesQuery, IReadOnlyList<KpiCategoryDto>>
{
    private readonly IAppDbContext _db;

    public GetKpiCategoriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KpiCategoryDto>> Handle(
        GetKpiCategoriesQuery req, CancellationToken ct)
    {
        var categoryTypeId = await _db.RefTypes
            .AsNoTracking()
            .Where(rt => rt.Code == "kpi_category")
            .Select(rt => rt.Id)
            .FirstOrDefaultAsync(ct);

        if (categoryTypeId == Guid.Empty)
            return [];

        var categories = await _db.RefValues
            .AsNoTracking()
            .Where(rv => rv.RefTypeId == categoryTypeId
                      && rv.IsActive
                      && rv.DeletedAt == null
                      && (rv.CorporationId == null || rv.CorporationId == req.CorporationId))
            .OrderBy(rv => rv.SortOrder)
            .ToListAsync(ct);

        return categories
            .Select(rv => new KpiCategoryDto(rv.Id, rv.Code, null))
            .ToList();
    }
}
