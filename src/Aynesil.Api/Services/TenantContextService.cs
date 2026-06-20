using System.Security.Claims;
using Aynesil.Application.Common.Interfaces;

namespace Aynesil.Api.Services;

/// <summary>
/// Resolves the current tenant (corporation + campus) from JWT claims.
/// Provides tenant context to the TenantConnectionInterceptor which sets
/// the PostgreSQL GUCs required for RLS.
/// Resolved per-request as a scoped service.
/// </summary>
public sealed class TenantContextService : ITenantContext
{
    private readonly IHttpContextAccessor _http;
    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public TenantContextService(IHttpContextAccessor http) => _http = http;

    public Guid? CorporationId =>
        Guid.TryParse(User?.FindFirstValue("corporation_id"), out var id) ? id : null;

    public Guid? CampusId =>
        Guid.TryParse(User?.FindFirstValue("campus_id"), out var id) ? id : null;

    public string? Locale =>
        User?.FindFirstValue("locale")
        ?? _http.HttpContext?.Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').First()
        ?? "tr";

    public string Timezone =>
        User?.FindFirstValue("timezone") ?? "Europe/Istanbul";
}
