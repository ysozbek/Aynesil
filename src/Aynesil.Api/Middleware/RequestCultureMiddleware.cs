using System.Globalization;
using System.Security.Claims;
using Aynesil.Application.Common.Localization;

namespace Aynesil.Api.Middleware;

/// <summary>
/// İstek kültürünü (<see cref="CultureInfo.CurrentUICulture"/>) ayarlar: Accept-Language (arayüz seçimi)
/// → JWT <c>locale</c> claim'i → tr. Pipeline'da iki kez çalışır (kimlik doğrulamadan önce ve sonra);
/// ikincisi claim'i görür. Yalnızca UI kültürü değiştirilir; sayı/tarih biçimleri etkilenmez.
/// </summary>
public sealed class RequestCultureMiddleware
{
    private readonly RequestDelegate _next;

    public RequestCultureMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        Apply(context);
        return _next(context);
    }

    /// <summary>
    /// Kültürü bu bağlam için çözüp geçerli akışa uygular. ExceptionMiddleware catch bloğunda da çağırır:
    /// iç middleware'lerin yaptığı AsyncLocal değişiklikleri üst çağırana yansımaz.
    /// </summary>
    public static CultureInfo Apply(HttpContext context)
    {
        var locale = LocaleResolver.Resolve(
            context.Request.Headers.AcceptLanguage.ToString(),
            context.User.FindFirstValue("locale"));
        var culture = CultureInfo.GetCultureInfo(locale);
        CultureInfo.CurrentUICulture = culture;
        return culture;
    }
}
