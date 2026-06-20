using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Queries;

// ── Request ───────────────────────────────────────────────────────────────────

/// <summary>
/// Paginated, filterable list of assessment templates.
/// Returns platform templates (corporation_id IS NULL) and the tenant's own templates.
/// is_active filter is optional — admin views may list inactive templates.
/// </summary>
public class GetAssessmentTemplatesQuery : PagedQuery, IRequest<PaginatedResult<AssessmentTemplateListItemDto>>
{
    public Guid? CorporationId { get; set; }
    public Guid? TypeId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetAssessmentTemplatesQueryHandler
    : IRequestHandler<GetAssessmentTemplatesQuery, PaginatedResult<AssessmentTemplateListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetAssessmentTemplatesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<AssessmentTemplateListItemDto>> Handle(
        GetAssessmentTemplatesQuery req, CancellationToken ct)
    {
        var query = AssessmentProjection.BuildTemplateListQuery(_db);

        // Platform templates (NULL corporation) + tenant's own
        if (req.CorporationId.HasValue)
            query = query.Where(t => t.CorporationId == null || t.CorporationId == req.CorporationId);

        if (req.TypeId.HasValue)
            query = query.Where(t => t.TypeId == req.TypeId);

        if (req.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == req.CategoryId);

        if (req.IsActive.HasValue)
            query = query.Where(t => t.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(t =>
                t.Code.Contains(req.Search) || t.Name.Contains(req.Search));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"    => req.IsDescending ? query.OrderByDescending(t => t.Code)    : query.OrderBy(t => t.Code),
            "name"    => req.IsDescending ? query.OrderByDescending(t => t.Name)    : query.OrderBy(t => t.Name),
            "version" => req.IsDescending ? query.OrderByDescending(t => t.Version) : query.OrderBy(t => t.Version),
            _         => query.OrderBy(t => t.Code).ThenByDescending(t => t.Version)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);

        return PaginatedResult<AssessmentTemplateListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
