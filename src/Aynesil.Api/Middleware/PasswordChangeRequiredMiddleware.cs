using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Aynesil.Shared.Constants;

namespace Aynesil.Api.Middleware;

/// <summary>
/// <c>must_change_password</c> açık hesapların (JWT "mcp" claim'i) erişimini kısıtlar: yalnızca parola değiştirme,
/// çıkış ve token yenileme uçlarına izin verilir; diğer her uç 403 <c>Auth.PasswordChangeRequired</c> döner.
/// Parola değişince tüm oturumlar iptal edilir; kullanıcı yeni parolasıyla girince token'da claim olmaz.
/// Anonim uçlar (giriş, parola sıfırlama) claim taşımadığı için etkilenmez.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/refresh",
    ];

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.FindFirst(AuthClaims.MustChangePassword)?.Value == "true"
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

    private static bool IsAllowed(PathString path) =>
        AllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
}
