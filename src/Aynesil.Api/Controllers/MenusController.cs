using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Menus.Commands;
using Aynesil.Application.Features.Menus.Dtos;
using Aynesil.Application.Features.Menus.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Dynamic menu management and user navigation tree.
///
/// Admin endpoints (menu:read / menu:manage) manage the full menu catalogue:
///   platform defaults (corporation_id = null) and tenant-scoped custom items.
///
/// The /me endpoint returns the current user's permission-filtered, locale-resolved
/// navigation tree — no additional permission gate required beyond authentication.
/// Menu visibility is entirely database-driven via required_permission_id on each item.
///
/// Never authorize by role name — all endpoints are permission-gated.
/// </summary>
[Route("api/menus")]
public sealed class MenusController : BaseController
{
    // ── User menu (public for any authenticated user) ─────────────────────────

    /// <summary>
    /// Returns the current user's permission-filtered navigation tree in the requested locale.
    /// Result is cached per corporation+locale and invalidated on any menu change.
    /// No additional permission gate — [Authorize] from BaseController is sufficient;
    /// the handler filters the tree by the caller's own permissions.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MenuTreeNodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMenu(
        [FromQuery] string? locale = null,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetUserMenuQuery { Locale = locale }, ct);
        return OkResult(result);
    }

    // ── Admin queries ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated flat list of menu items for admin management.
    /// Includes platform defaults and tenant-scoped items.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Menu.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MenuItemListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includePlatformDefaults = true,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetMenuItemsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            ParentId = parentId,
            IsActive = isActive,
            IncludePlatformDefaults = includePlatformDefaults,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>
    /// Returns the full menu catalogue as a depth-first ordered flat list for the admin tree editor.
    /// Use parentId to reconstruct the hierarchy client-side.
    /// </summary>
    [HttpGet("tree")]
    [HasPermission(Permissions.Menu.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MenuItemListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(
            new GetMenuTreeQuery { IncludeInactive = includeInactive }, ct);
        return OkResult(result);
    }

    /// <summary>Returns the full detail of a single menu item, including all translations.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Menu.Read)]
    [ProducesResponseType(typeof(ApiResponse<MenuItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMenuItemQuery(id), ct);
        return OkResult(result);
    }

    // ── Admin commands ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tenant-scoped menu item with initial translations.
    /// Platform default items (corporation_id = null) are seeded via migrations only.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MenuItemListItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateMenuItemCommand(
            req.ParentId,
            req.Code,
            req.Route,
            req.Icon,
            req.SortOrder,
            req.RequiredPermissionId,
            req.FeatureFlag,
            req.Translations
                .Select(t => new CreateMenuItemTranslationInput(t.Locale, t.Label))
                .ToList()), ct);

        return CreatedResult(result, $"/api/menus/{result.Id}");
    }

    /// <summary>
    /// Updates the structural properties of a menu item (parent, route, icon, sort order, permission gate).
    /// The code is immutable after creation. Use SetTranslations for label changes.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse<MenuItemListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuItemRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateMenuItemCommand(
            id,
            req.ParentId,
            req.Route,
            req.Icon,
            req.SortOrder,
            req.RequiredPermissionId,
            req.FeatureFlag,
            req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>
    /// Physically deletes a tenant-scoped menu item.
    /// Platform default items (corporation_id = null) cannot be deleted — deactivate them instead.
    /// Fails if the item has children.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteMenuItemCommand(id), ct);
        return NoContentResult("Menu item deleted.");
    }

    /// <summary>
    /// Replaces all translations for a menu item (upsert / replace-all strategy).
    /// Any locale not included in the request is removed.
    /// </summary>
    [HttpPut("{id:guid}/translations")]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MenuItemTranslationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetTranslations(
        Guid id,
        [FromBody] SetMenuItemTranslationsRequest req,
        CancellationToken ct)
    {
        var result = await Sender.Send(new SetMenuItemTranslationsCommand(
            id,
            req.Translations
                .Select(t => new SetMenuItemTranslationInput(t.Locale, t.Label))
                .ToList()), ct);

        return OkResult(result);
    }

    /// <summary>Makes the menu item visible in the navigation. Idempotent.</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateMenuItemCommand(id), ct);
        return NoContentResult("Menu item activated.");
    }

    /// <summary>Hides the menu item from navigation without deleting it. Idempotent.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Menu.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateMenuItemCommand(id), ct);
        return NoContentResult("Menu item deactivated.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record MenuTranslationRequest(string Locale, string Label);

public record CreateMenuItemRequest(
    Guid? ParentId,
    string Code,
    string? Route,
    string? Icon,
    int SortOrder,
    Guid? RequiredPermissionId,
    string? FeatureFlag,
    IReadOnlyList<MenuTranslationRequest> Translations);

public record UpdateMenuItemRequest(
    Guid? ParentId,
    string? Route,
    string? Icon,
    int SortOrder,
    Guid? RequiredPermissionId,
    string? FeatureFlag,
    int RowVersion);

public record SetMenuItemTranslationsRequest(
    IReadOnlyList<MenuTranslationRequest> Translations);
