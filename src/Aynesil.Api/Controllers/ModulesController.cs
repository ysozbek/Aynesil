using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>Oturum açan kullanıcının kurumunda açık isteğe bağlı modüller (kimlik doğrulama yeterli; izin gerekmez).</summary>
[Route("api/modules")]
public sealed class ModulesController : BaseController
{
    private readonly IModuleAccess _modules;

    public ModulesController(IModuleAccess modules) => _modules = modules;

    /// <summary>Açık modül kodları (<c>camps|consultancy|camera|surveys|care_team|kpi</c>). Durum her istekte okunur.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<EnabledModulesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var all = await _modules.GetAllAsync(ct);
        return OkResult(new EnabledModulesDto(all.Where(m => m.Value).Select(m => m.Key).Order().ToList()));
    }
}

public sealed record EnabledModulesDto(IReadOnlyList<string> EnabledModules);
