using System.Globalization;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aynesil.Api.RateLimiting;

/// <summary>
/// Eylemi <see cref="AuthRateLimiter"/> ile sınırlar. Hedef kimlik, bağlanmış istek gövdesindeki
/// <paramref name="identityProperty"/> özelliğinden okunur (ör. LoginRequest.Username). Sınır aşılırsa
/// 429 + Retry-After ve standart <see cref="ApiResponse"/> zarfı (kod: Error.TooManyRequests) döner.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AuthRateLimitAttribute(AuthRateLimitPolicy policy, string identityProperty) : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new AuthRateLimitFilter(policy, identityProperty, serviceProvider.GetRequiredService<AuthRateLimiter>());

    private sealed class AuthRateLimitFilter(
        AuthRateLimitPolicy policy, string identityProperty, AuthRateLimiter limiter) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var identity = context.ActionArguments.Values
                .Select(arg => arg?.GetType().GetProperty(identityProperty)?.GetValue(arg) as string)
                .FirstOrDefault(v => v is not null);
            var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            if (limiter.TryAcquire(policy, clientIp, identity, out var retryAfter))
            {
                await next();
                return;
            }

            if (retryAfter is { } wait)
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

            context.Result = new ObjectResult(
                ApiResponse.Fail(Msg.Get(MsgKeys.Error.TooManyRequests), MsgKeys.Error.TooManyRequests.Value)
                    with { TraceId = context.HttpContext.TraceIdentifier })
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
        }
    }
}
