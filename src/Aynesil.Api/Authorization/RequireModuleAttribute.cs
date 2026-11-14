using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aynesil.Api.Authorization;

/// <summary>
/// Kurum için kapatılmış modülün uçlarını 403 (<c>Error.ModuleDisabled</c>) ile reddeder.
/// Kimlik doğrulama/izin denetiminden sonra çalışır (yetkisiz istek yine 401/403 alır).
/// Modül kodları: <see cref="Aynesil.Shared.Constants.Modules"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireModuleAttribute : Attribute, IAsyncActionFilter
{
    public string Module { get; }

    public RequireModuleAttribute(string module) => Module = module;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var modules = context.HttpContext.RequestServices.GetRequiredService<IModuleAccess>();
        if (!await modules.IsEnabledAsync(Module, context.HttpContext.RequestAborted))
            throw new ForbiddenAccessException(MsgKeys.Error.ModuleDisabled);

        await next();
    }
}
