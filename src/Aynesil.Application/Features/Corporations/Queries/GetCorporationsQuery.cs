using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Corporations.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Queries;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>
/// Returns the caller's own corporation as a (single-row) paginated list (Y1). There is no platform
/// tier in the API: the handler filters by the session tenant and core.corporation RLS
/// (id = core.current_corporation_id()) enforces the same in the database.
/// </summary>
public class GetCorporationsQuery : PagedQuery, IRequest<PaginatedResult<CorporationListItemDto>>
{
    /// <summary>Filter by status: 'active', 'suspended', 'closed'. Null = all.</summary>
    public string? Status { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetCorporationsQueryHandler
    : IRequestHandler<GetCorporationsQuery, PaginatedResult<CorporationListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCorporationsQueryHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaginatedResult<CorporationListItemDto>> Handle(
        GetCorporationsQuery req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var query = _db.Corporations.AsNoTracking().Where(c => c.Id == corporationId);

        if (!string.IsNullOrWhiteSpace(req.Status))
            query = query.Where(c => c.Status == req.Status);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(c =>
                c.Code.Contains(req.Search) ||
                c.LegalName.Contains(req.Search) ||
                c.DisplayName.Contains(req.Search));

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"        => req.IsDescending ? query.OrderByDescending(c => c.Code)        : query.OrderBy(c => c.Code),
            "legalname"   => req.IsDescending ? query.OrderByDescending(c => c.LegalName)   : query.OrderBy(c => c.LegalName),
            "displayname" => req.IsDescending ? query.OrderByDescending(c => c.DisplayName) : query.OrderBy(c => c.DisplayName),
            "status"      => req.IsDescending ? query.OrderByDescending(c => c.Status)      : query.OrderBy(c => c.Status),
            "createdat"   => req.IsDescending ? query.OrderByDescending(c => c.CreatedAt)   : query.OrderBy(c => c.CreatedAt),
            _             => query.OrderBy(c => c.DisplayName)
        };

        var totalCount = await query.CountAsync(ct);

        var corporations = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        // Load campus counts in one query to avoid N+1
        var corpIds = corporations.Select(c => c.Id).ToList();
        var campusCounts = await _db.Campuses
            .AsNoTracking()
            .Where(cam => corpIds.Contains(cam.CorporationId))
            .GroupBy(cam => cam.CorporationId)
            .Select(g => new { CorporationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CorporationId, x => x.Count, ct);

        var items = corporations
            .Select(c => c.ToListItemDto(campusCounts.GetValueOrDefault(c.Id, 0)))
            .ToList();

        return PaginatedResult<CorporationListItemDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
