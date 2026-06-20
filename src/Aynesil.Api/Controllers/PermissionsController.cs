using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Permissions.Dtos;
using Aynesil.Application.Features.Permissions.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Read-only access to the platform-wide permission catalog.
/// Permissions are defined by the platform and cannot be created or deleted by tenants.
/// Tenants browse permissions to assign them to their custom roles.
/// </summary>
[Route("api/permissions")]
public sealed class PermissionsController : BaseController
{
    /// <summary>Returns a paginated, filterable list of all platform permissions.</summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PermissionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? search = null,
        [FromQuery] string? resource = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetPermissionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Resource = resource,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns a single permission by ID.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetPermissionQuery(id), ct);
        return OkResult(result);
    }
}
