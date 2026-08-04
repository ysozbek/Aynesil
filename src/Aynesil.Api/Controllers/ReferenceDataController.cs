using Aynesil.Api.Authorization;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Tenant-aware reference data lookup.
/// Returns effective ref_values for the current tenant (global values merged with
/// tenant overrides), cached for 30 minutes per (corporation, typeCode).
///
/// typeCode is case-insensitive: STUDENT_STATUS and student_status are equivalent.
/// Permission: ref_data:read required.
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
}
