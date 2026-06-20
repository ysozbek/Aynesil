using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Application.Features.Roles.Commands;
using Aynesil.Application.Features.Roles.Dtos;
using Aynesil.Application.Features.Roles.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Role management within the authenticated tenant.
/// Returns both tenant-specific roles and platform system role templates.
/// System roles (is_system=true) can have permissions adjusted but cannot be deleted.
/// Never authorize by role name — all endpoints are permission-gated.
/// </summary>
[Route("api/roles")]
public sealed class RolesController : BaseController
{
    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of roles visible to the current tenant.</summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RoleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool includeSystem = true,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetRolesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            IncludeSystem = includeSystem,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns full role details including all assigned permissions.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetRoleQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>Returns all permissions currently assigned to a role.</summary>
    [HttpGet("{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissions(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetRolePermissionsQuery(id), ct);
        return OkResult(result);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new tenant-scoped role. Role codes must be unique within the corporation.</summary>
    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    [ProducesResponseType(typeof(ApiResponse<RoleListItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateRoleCommand(req.Code, req.Name, req.Description), ct);
        return CreatedResult(result, $"/api/roles/{result.Id}");
    }

    /// <summary>Updates the name and description of an existing role. Code is immutable.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Roles.Update)]
    [ProducesResponseType(typeof(ApiResponse<RoleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateRoleCommand(id, req.Name, req.Description, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Soft-deletes a role. Fails if any user assignments remain.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Roles.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteRoleCommand(id), ct);
        return NoContentResult("Role deleted.");
    }

    /// <summary>Assigns a permission to a role. Idempotent — silently succeeds if already assigned.</summary>
    [HttpPost("{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.AssignPermission)]
    [ProducesResponseType(typeof(ApiResponse<PermissionListItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermission(Guid id, [FromBody] AssignRolePermissionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignRolePermissionCommand(id, req.PermissionId), ct);
        return CreatedResult(result, $"/api/roles/{id}/permissions");
    }

    /// <summary>Removes a permission from a role.</summary>
    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    [HasPermission(Permissions.Roles.AssignPermission)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken ct)
    {
        await Sender.Send(new RemoveRolePermissionCommand(id, permissionId), ct);
        return NoContentResult("Permission removed from role.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateRoleRequest(string Code, string Name, string? Description);

public record UpdateRoleRequest(string Name, string? Description, int RowVersion);

public record AssignRolePermissionRequest(Guid PermissionId);
