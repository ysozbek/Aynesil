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
        // Filter on the entity first — EF cannot translate Where on projected DTOs
        // that embed collection aggregates (Sections.Count).
        var q = _db.AssessmentTemplates.AsNoTracking();

        if (req.CorporationId.HasValue)
            q = q.Where(t => t.CorporationId == null || t.CorporationId == req.CorporationId);

        if (req.TypeId.HasValue)
            q = q.Where(t => t.TypeId == req.TypeId);

        if (req.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == req.CategoryId);

        if (req.IsActive.HasValue)
            q = q.Where(t => t.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            q = q.Where(t => t.Code.Contains(req.Search) || t.Name.Contains(req.Search));

        q = req.SortBy?.ToLowerInvariant() switch
        {
            "code"    => req.IsDescending ? q.OrderByDescending(t => t.Code)    : q.OrderBy(t => t.Code),
            "name"    => req.IsDescending ? q.OrderByDescending(t => t.Name)    : q.OrderBy(t => t.Name),
            "version" => req.IsDescending ? q.OrderByDescending(t => t.Version) : q.OrderBy(t => t.Version),
            _         => q.OrderBy(t => t.Code).ThenByDescending(t => t.Version)
        };

        var total = await q.CountAsync(ct);
        var items = await AssessmentProjection.ProjectTemplateList(_db, q)
            .Skip(req.Skip).Take(req.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<AssessmentTemplateListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}
