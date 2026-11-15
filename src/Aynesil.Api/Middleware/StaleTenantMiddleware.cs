using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Localization;
using Aynesil.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Rejects JWTs whose corporation_id no longer exists (e.g. after a local DB reset) or whose corporation is
/// no longer active (suspended/closed/deleted, V57). Without the first check, RLS silently returns empty lists
/// for campuses/users/roles while platform menus and corporation catalogue still appear to work. The second makes
/// suspending a tenant take effect for already-issued access tokens within <see cref="CacheDuration"/> instead of
/// at token expiry; login and refresh check the status themselves (LoginCommandHandler, JwtTokenService).
/// Both answer 401 so the client drops the session (its refresh attempt is refused with the same message).
/// </summary>
public sealed class StaleTenantMiddleware
{
    /// <summary>Per-instance cache of the corporation's state; bounds how long a suspended tenant keeps working.</summary>
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private enum CorporationState { Missing, Active, Inactive }

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
            var cacheKey = $"corp-state:{corporationId:D}";
            if (!cache.TryGetValue(cacheKey, out CorporationState state))
            {
                // RLS: the tenant GUC is this corporation, so only its own row is visible.
                var row = await db.Corporations
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(c => c.Id == corporationId)
                    .Select(c => new { c.Status, c.DeletedAt })
                    .FirstOrDefaultAsync(context.RequestAborted);

                state = row is null ? CorporationState.Missing
                    : row.Status == "active" && row.DeletedAt == null ? CorporationState.Active
                    : CorporationState.Inactive;
                cache.Set(cacheKey, state, CacheDuration);
            }

            if (state != CorporationState.Active)
            {
                var key = state == CorporationState.Missing ? MsgKeys.Error.TenantSessionInvalid : MsgKeys.Auth.CorporationInactive;
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse.Fail(Msg.Get(key), key.Value) with { TraceId = context.TraceIdentifier });
                return;
            }
        }

        await _next(context);
    }
}
