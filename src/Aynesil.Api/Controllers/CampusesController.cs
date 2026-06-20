using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Campuses.Commands;
using Aynesil.Application.Features.Campuses.Dtos;
using Aynesil.Application.Features.Campuses.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Campus (branch) management.
/// A campus is an authorization sub-scope within a corporation, not an isolation boundary.
/// Route: /api/campuses  (flat resource — corporate nesting is expressed via query/body CorporationId).
/// </summary>
[Route("api/campuses")]
public sealed class CampusesController : BaseController
{
    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filterable list of campuses (branches).
    /// Scope to a corporation by providing <paramref name="corporationId"/>.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Campus.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CampusListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCampusesQuery
        {
            CorporationId = corporationId,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns the full details of a single campus (branch).</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Campus.Read)]
    [ProducesResponseType(typeof(ApiResponse<CampusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetCampusQuery(id), ct);
        return OkResult(result);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new campus (branch) under the specified corporation.</summary>
    [HttpPost]
    [HasPermission(Permissions.Campus.Create)]
    [ProducesResponseType(typeof(ApiResponse<CampusDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateCampusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateCampusCommand(
            req.CorporationId,
            req.Code,
            req.Name,
            req.City,
            req.AddressLine,
            req.District,
            req.Phone,
            req.Email,
            req.Timezone,
            req.GeoLat,
            req.GeoLng), ct);

        return CreatedResult(result, $"/api/campuses/{result.Id}");
    }

    /// <summary>
    /// Updates a campus's details. Code is immutable after creation.
    /// Use activate/deactivate endpoints to change active status.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Campus.Update)]
    [ProducesResponseType(typeof(ApiResponse<CampusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCampusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCampusCommand(
            id,
            req.Name,
            req.City,
            req.AddressLine,
            req.District,
            req.Phone,
            req.Email,
            req.Timezone,
            req.GeoLat,
            req.GeoLng,
            req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Soft-deletes the campus. Historical data referencing this campus is preserved.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Campus.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCampusCommand(id), ct);
        return NoContentResult("Campus deleted.");
    }

    /// <summary>Activates a previously deactivated campus.</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Campus.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateCampusCommand(id), ct);
        return NoContentResult("Campus activated.");
    }

    /// <summary>Deactivates the campus. Existing data is preserved; new operations referencing it are blocked.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Campus.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateCampusCommand(id), ct);
        return NoContentResult("Campus deactivated.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateCampusRequest(
    Guid CorporationId,
    string Code,
    string Name,
    string? City,
    string? AddressLine,
    string? District,
    string? Phone,
    string? Email,
    string? Timezone,
    decimal? GeoLat,
    decimal? GeoLng);

public record UpdateCampusRequest(
    string Name,
    string? City,
    string? AddressLine,
    string? District,
    string? Phone,
    string? Email,
    string? Timezone,
    decimal? GeoLat,
    decimal? GeoLng,
    int RowVersion);
