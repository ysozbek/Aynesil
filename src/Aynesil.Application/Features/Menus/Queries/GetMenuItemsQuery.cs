using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Menus.Queries;

/// <summary>
/// Returns a paginated flat list of menu items for the admin management UI.
/// Includes both platform defaults and tenant-scoped items visible to the current corporation.
/// </summary>
public class GetMenuItemsQuery : PagedQuery, IRequest<PaginatedResult<MenuItemListItemDto>>
{
    /// <summary>Filter by parent item. Null returns root-level items and all when combined with a search.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>When true, filters to active items only. Null returns all.</summary>
    public bool? IsActive { get; set; }

    /// <summary>When true (default), includes platform default items alongside tenant-scoped items.</summary>
    public bool IncludePlatformDefaults { get; set; } = true;
}

public sealed class GetMenuItemsQueryHandler : IRequestHandler<GetMenuItemsQuery, PaginatedResult<MenuItemListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMenuItemsQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedResult<MenuItemListItemDto>> Handle(GetMenuItemsQuery req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var query = _db.MenuItems
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .Where(m => m.CorporationId == corporationId ||
                        (req.IncludePlatformDefaults && m.CorporationId == null));

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.ToLowerInvariant();
            query = query.Where(m =>
                m.Code.Contains(term) ||
                (m.Route != null && m.Route.Contains(term)) ||
                m.Translations.Any(t => t.Label.ToLower().Contains(term)));
        }

        if (req.ParentId.HasValue)
            query = query.Where(m => m.ParentId == req.ParentId);

        if (req.IsActive.HasValue)
            query = query.Where(m => m.IsActive == req.IsActive);

        var total = await query.CountAsync(ct);

        query = req.SortBy?.ToLowerInvariant() switch
        {
            "code"      => req.IsDescending ? query.OrderByDescending(m => m.Code) : query.OrderBy(m => m.Code),
            "sortorder" => req.IsDescending ? query.OrderByDescending(m => m.SortOrder) : query.OrderBy(m => m.SortOrder),
            "createdat" => req.IsDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
            _           => query.OrderBy(m => m.SortOrder).ThenBy(m => m.Code)
        };

        var items = await query
            .Skip(req.Skip)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return PaginatedResult<MenuItemListItemDto>.Create(
            items.Select(m => m.ToListItemDto()).ToList(),
            total,
            req.Page,
            req.PageSize);
    }
}
