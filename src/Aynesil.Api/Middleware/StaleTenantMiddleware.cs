using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Rejects JWTs whose corporation_id no longer exists (e.g. after a local DB reset).
/// Without this, RLS silently returns empty lists for campuses/users/roles while
/// platform menus and corporation catalogue still appear to work.
/// </summary>
public sealed class StaleTenantMiddleware
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;

    public StaleTenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenant,
        IAppDbContext db,
        IMemoryCache cache)
    {
        // Login/refresh/logout must work even when the access token's tenant was wiped
        // (e.g. local DB reset). The axios client still attaches Authorization on refresh.
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true
            && tenant.CorporationId is Guid corporationId)
        {
            var cacheKey = $"corp-exists:{corporationId:D}";
            if (!cache.TryGetValue(cacheKey, out bool exists))
            {
                exists = await db.Corporations
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == corporationId, context.RequestAborted);

                cache.Set(cacheKey, exists, CacheDuration);
            }

            if (!exists)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.Fail("Tenant session is no longer valid. Please sign in again.")
                        with { TraceId = context.TraceIdentifier });
                return;
            }
        }

        await _next(context);
    }
}
