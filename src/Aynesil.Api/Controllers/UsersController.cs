using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Users.Commands;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Application.Features.Users.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// User account management within the authenticated tenant.
/// All operations are scoped to the current corporation via JWT + RLS.
/// Never authorize by role name — all endpoints are permission-gated.
/// </summary>
[Route("api/users")]
public sealed class UsersController : BaseController
{
    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>Returns a paginated, searchable list of users in the current corporation.</summary>
    [HttpGet]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            RoleId = roleId,
            CampusId = campusId,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns the full profile of a single user.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetUserQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>Returns all role grants currently assigned to a user.</summary>
    [HttpGet("{id:guid}/roles")]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoles(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetUserRolesQuery(id), ct);
        return OkResult(result);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new user account within the current corporation. Activates the account immediately.</summary>
    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateUserCommand(
            req.Username, req.FullName, req.Email, req.Phone,
            req.Password, req.PreferredLocale, req.PrimaryCampusId,
            ActivateImmediately: true), ct);

        return CreatedResult(result, $"/api/users/{result.Id}");
    }

    /// <summary>Updates a user's profile fields. Username and password are immutable via this endpoint.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateUserCommand(
            id, req.FullName, req.Phone, req.Email,
            req.PreferredLocale, req.PrimaryCampusId, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Soft-deletes the user. All active sessions are revoked immediately.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Users.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int rowVersion, CancellationToken ct)
    {
        await Sender.Send(new DeleteUserCommand(id, rowVersion), ct);
        return NoContentResult("User deleted.");
    }

    /// <summary>Transitions the user's status to 'active'.</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateUserCommand(id), ct);
        return NoContentResult("User activated.");
    }

    /// <summary>Transitions the user's status to 'suspended'. Active sessions are revoked.</summary>
    [HttpPost("{id:guid}/suspend")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SuspendUserCommand(id), ct);
        return NoContentResult("User suspended.");
    }

    /// <summary>Grants a role to a user, optionally scoped to a specific campus.</summary>
    [HttpPost("{id:guid}/roles")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserRoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignUserRoleRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AssignUserRoleCommand(
            id, req.RoleId, req.CampusId, req.ValidFrom, req.ValidTo), ct);

        return CreatedResult(result, $"/api/users/{id}/roles");
    }

    /// <summary>Removes a specific role grant from a user.</summary>
    [HttpDelete("{id:guid}/roles/{userRoleId:guid}")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, Guid userRoleId, CancellationToken ct)
    {
        await Sender.Send(new RemoveUserRoleCommand(userRoleId), ct);
        return NoContentResult("Role removed.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateUserRequest(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string? Password,
    string? PreferredLocale,
    Guid? PrimaryCampusId);

public record UpdateUserRequest(
    string FullName,
    string? Phone,
    string? Email,
    string? PreferredLocale,
    Guid? PrimaryCampusId,
    int RowVersion);

public record AssignUserRoleRequest(
    Guid RoleId,
    Guid? CampusId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);
