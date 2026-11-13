using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Aynesil.Api.Middleware;

/// <summary>
/// <c>must_change_password</c> açık hesapların (JWT "mcp" claim'i) erişimini kısıtlar: yalnızca parola değiştirme,
/// çıkış ve token yenileme uçlarına izin verilir; diğer her uç 403 <c>Auth.PasswordChangeRequired</c> döner.
/// Parola değişince tüm oturumlar iptal edilir; kullanıcı yeni parolasıyla girince token'da claim olmaz.
/// Anonim uçlar (<c>[AllowAnonymous]</c>: giriş, token yenileme, parola sıfırlama) her zaman serbesttir; istemci eski bir
/// "mcp" token'ını başlıkta taşısa bile giriş engellenmez.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    // Oturum gerektiren iki uç; anonim uçlar (login/refresh/sıfırlama) endpoint meta verisinden serbest bırakılır.
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/logout",
    ];

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.FindFirst(AuthClaims.MustChangePassword)?.Value == "true"
            && !IsAnonymousEndpoint(context)
            && !IsAllowed(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail(Msg.Get(MsgKeys.Auth.PasswordChangeRequired), MsgKeys.Auth.PasswordChangeRequired.Value)
                    with { TraceId = context.TraceIdentifier });
            return;
        }

        await _next(context);
    }

    private static bool IsAnonymousEndpoint(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

    private static bool IsAllowed(PathString path) =>
        AllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
}
