using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Menus.Queries;

/// <summary>
/// Returns the complete flat list of menu items for admin tree management.
/// Includes platform defaults and all tenant items (active and inactive).
/// The client uses ParentId to render the tree and drag-and-drop reordering.
/// </summary>
public class GetMenuTreeQuery : IRequest<IReadOnlyList<MenuItemListItemDto>>
{
    /// <summary>When false (default), returns active items only. Set true to include inactive items.</summary>
    public bool IncludeInactive { get; set; } = false;
}

public sealed class GetMenuTreeQueryHandler : IRequestHandler<GetMenuTreeQuery, IReadOnlyList<MenuItemListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMenuTreeQueryHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<MenuItemListItemDto>> Handle(GetMenuTreeQuery req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var query = _db.MenuItems
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .Where(m => m.CorporationId == null || m.CorporationId == corporationId);

        if (!req.IncludeInactive)
            query = query.Where(m => m.IsActive);

        // Order for depth-first traversal: root items first (parentId null), then by sort order
        var items = await query
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Code)
            .ToListAsync(ct);

        // Re-order as depth-first traversal so the list is tree-ordered
        var ordered = OrderDepthFirst(items, null);

        return ordered.Select(m => m.ToListItemDto()).ToList();
    }

    private static List<Domain.Modules.Iam.Entities.MenuItem> OrderDepthFirst(
        List<Domain.Modules.Iam.Entities.MenuItem> all,
        Guid? parentId)
    {
        var result = new List<Domain.Modules.Iam.Entities.MenuItem>();
        foreach (var item in all.Where(m => m.ParentId == parentId))
        {
            result.Add(item);
            result.AddRange(OrderDepthFirst(all, item.Id));
        }
        return result;
    }
}
