using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Campuses.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Queries;

// ── Request ──────────────────────────────────────────────────────────────────
/// <summary>
/// Returns a paginated, filterable list of campuses (branches).
/// When CorporationId is provided, results are scoped to that corporation.
/// RLS further restricts rows to the current tenant context when the user is tenant-scoped.
/// </summary>
public class GetCampusesQuery : PagedQuery, IRequest<PaginatedResult<CampusListItemDto>>
{
    /// <summary>Filter by corporation. Null = all visible corporations (platform admin only).</summary>
    public Guid? CorporationId { get; set; }

    /// <summary>Filter by active status. Null = all.</summary>
    public bool? IsActive { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetCampusesQueryHandler
    : IRequestHandler<GetCampusesQuery, PaginatedResult<CampusListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCampusesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<CampusListItemDto>> Handle(
        GetCampusesQuery req, CancellationToken ct)
    {
        var query = _db.Campuses
            .AsNoTracking()
            .Include(c => c.Corporation);

        var filtered = query.AsQueryable();

        if (req.CorporationId.HasValue)
            filtered = filtered.Where(c => c.CorporationId == req.CorporationId.Value);

        if (req.IsActive.HasValue)
            filtered = filtered.Where(c => c.IsActive == req.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
            filtered = filtered.Where(c =>
                c.Code.Contains(req.Search) ||
                c.Name.Contains(req.Search) ||
                (c.City != null && c.City.Contains(req.Search)));

        filtered = req.SortBy?.ToLowerInvariant() switch
        {
            "code"     => req.IsDescending ? filtered.OrderByDescending(c => c.Code)     : filtered.OrderBy(c => c.Code),
            "name"     => req.IsDescending ? filtered.OrderByDescending(c => c.Name)     : filtered.OrderBy(c => c.Name),
            "city"     => req.IsDescending ? filtered.OrderByDescending(c => c.City)     : filtered.OrderBy(c => c.City),
            "isactive" => req.IsDescending ? filtered.OrderByDescending(c => c.IsActive) : filtered.OrderBy(c => c.IsActive),
            "createdat"=> req.IsDescending ? filtered.OrderByDescending(c => c.CreatedAt): filtered.OrderBy(c => c.CreatedAt),
            _          => filtered.OrderBy(c => c.Name)
        };

        var totalCount = await filtered.CountAsync(ct);

        var campuses = await filtered
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var items = campuses
            .Select(c => c.ToListItemDto(c.Corporation?.DisplayName ?? string.Empty))
            .ToList();

        return PaginatedResult<CampusListItemDto>.Create(items, totalCount, req.Page, req.PageSize);
    }
}
