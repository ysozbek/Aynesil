using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Shared.Constants;
using MediatR;

namespace Aynesil.Application.Features.Menus.Queries;

/// <summary>
/// Returns the permission-filtered, locale-resolved recursive navigation tree for the current user.
/// Result is corporation+locale scoped and cached; cache is invalidated on any menu change.
/// Menu items whose required_permission_id the user does not hold are silently excluded from the tree.
/// Parent items with no visible children are also excluded to avoid empty sections.
/// </summary>
public class GetUserMenuQuery : IRequest<IReadOnlyList<MenuTreeNodeDto>>
{
    /// <summary>
    /// BCP-47 locale code to resolve menu labels (e.g. 'tr', 'en').
    /// Falls back to the tenant context locale, then to the first available translation.
    /// </summary>
    public string? Locale { get; set; }
}

public sealed class GetUserMenuQueryHandler : IRequestHandler<GetUserMenuQuery, IReadOnlyList<MenuTreeNodeDto>>
{
    private readonly IMenuRepository _menuRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    public GetUserMenuQueryHandler(
        IMenuRepository menuRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _menuRepository = menuRepository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<IReadOnlyList<MenuTreeNodeDto>> Handle(GetUserMenuQuery req, CancellationToken ct)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var locale = req.Locale
            ?? _tenantContext.Locale
            ?? "tr";

        // Load the full active flat list from cache (unfiltered by user permissions)
        var cacheKey = CacheKeys.MenuTree(corporationId, locale);
        var allItems = await _cache.GetOrSetAsync(
            cacheKey,
            async innerCt =>
            {
                var items = await _menuRepository.GetActiveFlatListAsync(corporationId, innerCt);
                return items.Select(m => new CachedMenuEntry(
                    m.Id,
                    m.ParentId,
                    m.Code,
                    m.Translations.FirstOrDefault(t => t.Locale == locale)?.Label
                        ?? m.Translations.FirstOrDefault()?.Label
                        ?? m.Code,
                    m.Route,
                    m.Icon,
                    m.SortOrder,
                    m.RequiredPermission?.Code))
                .ToList();
            },
            CacheExpiry,
            ct);

        // Filter in-memory using the current user's permissions (read from JWT claims — no DB hit)
        var visible = allItems
            .Where(m => m.RequiredPermissionCode is null ||
                        _currentUser.HasPermission(m.RequiredPermissionCode))
            .ToList();

        // Build the recursive tree, pruning parent nodes with no visible children
        return BuildTree(visible, null);
    }

    /// <summary>
    /// Recursively builds the menu tree from the flat visible list.
    /// Parent nodes with zero visible children are excluded (avoids empty sections).
    /// </summary>
    private static IReadOnlyList<MenuTreeNodeDto> BuildTree(
        IReadOnlyList<CachedMenuEntry> visible,
        Guid? parentId)
    {
        var result = new List<MenuTreeNodeDto>();

        foreach (var item in visible.Where(m => m.ParentId == parentId).OrderBy(m => m.SortOrder))
        {
            var children = BuildTree(visible, item.Id);

            // Include this item if it has a route (leaf) OR has visible children (section)
            if (item.Route is not null || children.Count > 0)
            {
                result.Add(new MenuTreeNodeDto(
                    item.Id,
                    item.Code,
                    item.Label,
                    item.Route,
                    item.Icon,
                    item.SortOrder,
                    children));
            }
        }

        return result.AsReadOnly();
    }
}

// ── Internal cache entry ──────────────────────────────────────────────────────

/// <summary>
/// Compact representation of a menu item stored in the cache.
/// Contains the locale-resolved label and the permission code for fast in-memory filtering.
/// </summary>
internal sealed record CachedMenuEntry(
    Guid Id,
    Guid? ParentId,
    string Code,
    string Label,
    string? Route,
    string? Icon,
    int SortOrder,
    string? RequiredPermissionCode);
