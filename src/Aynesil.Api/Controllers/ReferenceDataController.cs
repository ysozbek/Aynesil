using Aynesil.Api.Authorization;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.RefData.Commands;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Tenant-aware reference data lookup and management.
/// Returns effective ref_values for the current tenant (global values merged with
/// tenant overrides), cached for 30 minutes per (corporation, typeCode).
///
/// typeCode is case-insensitive: STUDENT_STATUS and student_status are equivalent.
/// Read: ref_data:read. Write: ref_data:manage.
/// </summary>
[Route("api/reference-data")]
public sealed class ReferenceDataController : BaseController
{
    private readonly IRefDataService _refData;

    public ReferenceDataController(IRefDataService refData) => _refData = refData;

    /// <summary>
    /// Returns all effective reference values for the given type code.
    /// </summary>
    [HttpGet("{typeCode}")]
    [HasPermission(Permissions.RefData.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RefValueDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValues(
        string typeCode,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        // Accept both UPPER_SNAKE_CASE and lower_snake_case — DB stores lowercase codes.
        var normalised = typeCode.ToLowerInvariant().Replace('-', '_');
        var values = await _refData.GetValuesAsync(normalised, activeOnly, ct);
        return OkResult(values);
    }

    /// <summary>
    /// Returns the default value for the given type code.
    /// </summary>
    [HttpGet("{typeCode}/default")]
    [HasPermission(Permissions.RefData.Read)]
    [ProducesResponseType(typeof(ApiResponse<RefValueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefault(string typeCode, CancellationToken ct)
    {
        var normalised = typeCode.ToLowerInvariant().Replace('-', '_');
        var value = await _refData.GetDefaultAsync(normalised, ct);
        return OkResult(value);
    }

    /// <summary>
    /// Creates a tenant-scoped reference value (requires allows_tenant_values).
    /// </summary>
    [HttpPost("{typeCode}")]
    [HasPermission(Permissions.RefData.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RefValueDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        string typeCode,
        [FromBody] CreateRefValueRequest body,
        CancellationToken ct)
    {
        var result = await Sender.Send(new CreateRefValueCommand(
            typeCode, body.Code, body.Label, body.SortOrder, body.IsDefault), ct);
        return CreatedResult(result, $"/api/reference-data/values/{result.Id}");
    }

    /// <summary>
    /// Updates a tenant-owned reference value (label, sort order, default).
    /// Shared/global rows cannot be edited directly.
    /// </summary>
    [HttpPut("values/{id:guid}")]
    [HasPermission(Permissions.RefData.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RefValueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRefValueRequest body,
        CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateRefValueCommand(
            id, body.Label, body.SortOrder, body.IsDefault), ct);
        return OkResult(result);
    }

    /// <summary>
    /// Activates or deactivates a value for the current tenant.
    /// Own rows update is_active; shared rows upsert ref_value_tenant_override.
    /// </summary>
    [HttpPut("values/{id:guid}/active")]
    [HasPermission(Permissions.RefData.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetActive(
        Guid id,
        [FromBody] SetRefValueActiveRequest body,
        CancellationToken ct)
    {
        await Sender.Send(new SetRefValueActiveCommand(id, body.IsActive), ct);
        return NoContentResult();
    }
}

public sealed record CreateRefValueRequest(
    string Code,
    string Label,
    int SortOrder = 0,
    bool IsDefault = false);

public sealed record UpdateRefValueRequest(
    string Label,
    int SortOrder = 0,
    bool IsDefault = false);

public sealed record SetRefValueActiveRequest(bool IsActive);
