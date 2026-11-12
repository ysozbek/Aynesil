using Aynesil.Domain.Common;
using System.Text.Json;
using Aynesil.Application.Common.Localization;
using Aynesil.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Aynesil.Api.Authorization;

/// <summary>
/// [HasPermission] reddinde (403) gövdesiz yanıt yerine ExceptionMiddleware ile aynı
/// <c>{ success:false, message }</c> zarfını döner. Diğer sonuçlar (401 challenge dahil)
/// varsayılan işleyiciye bırakılır.
/// </summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && !context.Response.HasStarted)
        {
            var payload = ApiResponse.Fail(Msg.Get(MsgKeys.Error.Forbidden), MsgKeys.Error.Forbidden.Value) with { TraceId = context.TraceIdentifier };
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
