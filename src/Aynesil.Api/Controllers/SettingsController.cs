using System.Text.Json;
using Aynesil.Application.Features.Settings.Queries;
using Aynesil.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Effective settings for the current authenticated tenant/user.
/// Loaded by the app shell — authentication is sufficient (no settings:read gate).
/// </summary>
[Route("api/settings")]
public sealed class SettingsController : BaseController
{
    /// <summary>
    /// Returns a flat map of setting key → effective value for the current caller.
    /// Resolution order: user → corporation → system → definition default.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, JsonElement?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await Sender.Send(new GetEffectiveSettingsQuery(), ct);
        return OkResult(result);
    }
}
