using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aynesil.Api.Middleware;

/// <summary>
/// Kullanıcı düzeyinde oturum denetimi (V63; StaleTenantMiddleware'in kullanıcı karşılığı). Erişim jetonu imzası
/// geçerli olsa bile şu durumlarda 401 <c>Auth.SessionRevoked</c> döner:
///   - kullanıcı silinmiş, bulunamıyor ya da durumu <c>active</c> değil (askıya alınmış / devre dışı);
///   - jetonun verildiği an (<c>iat_ms</c>, ms) <c>iam.user_account.sessions_revoked_at</c>'ten önce (şifre
///     değişimi/sıfırlama, askıya alma, silme, rol/izin değişikliği). Her iki değer de API saatinden gelir; iptalden
///     hemen sonraki yeni giriş geçer, iptalle aynı saniyede ama önce verilmiş jeton reddedilir.
/// İstemci 401'de yenilemeyi dener: yenileme jetonu iptal edilmemişse (rol/izin değişikliği) güncel izinlerle yeni
/// erişim jetonu alır; iptal edilmişse (şifre, askıya alma, silme) giriş ekranına döner.
/// Durum kullanıcı başına <see cref="CacheDuration"/> önbelleklenir; değiştiren işlemler önbelleği temizler
/// (<see cref="ISessionRevocation"/>). Anonim uçlar (giriş, yenileme, çıkış) denetlenmez.
/// </summary>
public sealed class UserSessionMiddleware
{
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private sealed record UserState(bool Usable, DateTimeOffset? SessionsRevokedAt);

    private readonly RequestDelegate _next;

    public UserSessionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser, IAppDbContext db, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || currentUser.UserId is not Guid userId)
        {
            await _next(context);
            return;
        }

        var key = CacheKeys.UserSessionState(userId);
        if (!cache.TryGetValue(key, out UserState? state) || state is null)
        {
            // RLS: kurum GUC'u jetondaki kurumdur; başka kurumun kullanıcısı görünmez (→ reddedilir).
            var row = await db.UserAccounts
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Status, u.DeletedAt, u.SessionsRevokedAt })
                .FirstOrDefaultAsync(context.RequestAborted);
            state = new UserState(row is not null && row.DeletedAt == null && row.Status == "active", row?.SessionsRevokedAt);
            cache.Set(key, state, CacheDuration);
        }

        if (!state.Usable || IssuedBeforeRevocation(context, state.SessionsRevokedAt))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail(Msg.Get(MsgKeys.Auth.SessionRevoked), MsgKeys.Auth.SessionRevoked.Value)
                    with { TraceId = context.TraceIdentifier });
            return;
        }

        await _next(context);
    }

    /// <summary><c>iat_ms</c> yoksa (V63'ten önce verilmiş jeton) iptal kaydı varken geçersiz sayılır.</summary>
    private static bool IssuedBeforeRevocation(HttpContext context, DateTimeOffset? revokedAt)
    {
        if (revokedAt is null) return false;
        var issued = context.User.FindFirst(AuthClaims.IssuedAtMs)?.Value;
        return !long.TryParse(issued, out var issuedAtMs) || issuedAtMs < revokedAt.Value.ToUnixTimeMilliseconds();
    }
}
