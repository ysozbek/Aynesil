using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Corporations.Commands;
using Aynesil.Application.Features.Corporations.Dtos;
using Aynesil.Application.Features.Corporations.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Platform-level corporation (tenant) management.
/// Corporation = tenant root; only platform administrators should hold Corporation permissions.
/// All endpoints require the corresponding permission — never authorize by role name.
/// </summary>
[Route("api/corporations")]
public sealed class CorporationsController : BaseController
{
    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>Returns a paginated, searchable list of all corporations.</summary>
    [HttpGet]
    [HasPermission(Permissions.Corporation.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CorporationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCorporationsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            SortBy = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns the full details of a single corporation.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Corporation.Read)]
    [ProducesResponseType(typeof(ApiResponse<CorporationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetCorporationQuery(id), ct);
        return OkResult(result);
    }

    /// <summary>Returns the settings and locale preferences of a corporation.</summary>
    [HttpGet("{id:guid}/settings")]
    [HasPermission(Permissions.Corporation.Read)]
    [ProducesResponseType(typeof(ApiResponse<CorporationSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettings(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetCorporationSettingsQuery(id), ct);
        return OkResult(result);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Creates a new corporation (tenant). The code slug must be globally unique.</summary>
    [HttpPost]
    [HasPermission(Permissions.Corporation.Create)]
    [ProducesResponseType(typeof(ApiResponse<CorporationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateCorporationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateCorporationCommand(
            req.Code,
            req.LegalName,
            req.DisplayName,
            req.DefaultLocale,
            req.DefaultCurrency,
            req.Timezone,
            req.TaxOffice,
            req.TaxNumber), ct);

        return CreatedResult(result, $"/api/corporations/{result.Id}");
    }

    /// <summary>Updates the corporation's identity and locale fields. Code is immutable.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Corporation.Update)]
    [ProducesResponseType(typeof(ApiResponse<CorporationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCorporationRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCorporationCommand(
            id,
            req.LegalName,
            req.DisplayName,
            req.DefaultLocale,
            req.DefaultCurrency,
            req.Timezone,
            req.TaxOffice,
            req.TaxNumber,
            req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Updates the corporation's settings blob and locale/currency/timezone preferences.</summary>
    [HttpPut("{id:guid}/settings")]
    [HasPermission(Permissions.Corporation.Update)]
    [ProducesResponseType(typeof(ApiResponse<CorporationSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSettings(
        Guid id, [FromBody] UpdateCorporationSettingsRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCorporationSettingsCommand(
            id,
            req.DefaultLocale,
            req.DefaultCurrency,
            req.Timezone,
            req.TaxOffice,
            req.TaxNumber,
            req.Settings,
            req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Soft-deletes the corporation. All tenant data remains in the DB.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Corporation.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteCorporationCommand(id), ct);
        return NoContentResult("Corporation deleted.");
    }

    /// <summary>Transitions the corporation status to 'active'.</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Corporation.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivateCorporationCommand(id), ct);
        return NoContentResult("Corporation activated.");
    }

    /// <summary>Transitions the corporation status to 'suspended'.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Corporation.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivateCorporationCommand(id), ct);
        return NoContentResult("Corporation suspended.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateCorporationRequest(
    string Code,
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber);

public record UpdateCorporationRequest(
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    int RowVersion);

public record UpdateCorporationSettingsRequest(
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    string Settings,
    int RowVersion);
