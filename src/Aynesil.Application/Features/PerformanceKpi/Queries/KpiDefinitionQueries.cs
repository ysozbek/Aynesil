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

        // Build base join with anonymous type — sort on entity properties BEFORE projection.
        var baseQ =
            from k in q
            join cat in _db.RefValues.AsNoTracking()
                on k.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            select new { k, cat };

        var sortedQ = req.SortBy?.ToLowerInvariant() switch
        {
            "name"   => req.IsDescending ? baseQ.OrderByDescending(x => x.k.Name)    : baseQ.OrderBy(x => x.k.Name),
            "code"   => req.IsDescending ? baseQ.OrderByDescending(x => x.k.Code)    : baseQ.OrderBy(x => x.k.Code),
            "active" => req.IsDescending ? baseQ.OrderByDescending(x => x.k.IsActive) : baseQ.OrderBy(x => x.k.IsActive),
            _        => baseQ.OrderBy(x => x.k.Code)
        };

        var total = await sortedQ.CountAsync(ct);
        var items = await sortedQ
            .Skip(req.Skip).Take(req.PageSize)
            .Select(x => new KpiDefinitionListItemDto(
                x.k.Id, x.k.CorporationId, x.k.Code, x.k.Name,
                x.k.CategoryId, x.cat != null ? x.cat.Code : null,
                x.k.Unit, x.k.IsActive, x.k.UpdatedAt))
            .ToListAsync(ct);
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
