using Aynesil.Domain.Common;
using Aynesil.Api;
using Aynesil.Api.Authorization;
using Aynesil.Api.Middleware;
using Aynesil.Api.Services;
using Aynesil.Application;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Localization;
using Aynesil.Infrastructure;
using Aynesil.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

// ── Bootstrap Serilog early (captures startup failures) ─────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── AYNESIL_ prefix env var support ──────────────────────────────────────
    // Varsayılan CreateBuilder() tüm env var'ları prefix olmadan okur.
    // Bu satır ile "AYNESIL_ConnectionStrings__DefaultConnection" →
    //   "ConnectionStrings:DefaultConnection" olarak map'lenir.
    // Docker Compose env section'daki AYNESIL_ prefix'li değerler böylece
    // GetConnectionString("DefaultConnection") ile doğru okunur.
    builder.Configuration.AddEnvironmentVariables("AYNESIL_");

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // ── Servis kayıtları (test edilebilir: ServiceRegistrationTests Development ayarlarıyla doğrular) ──
    builder.Services.AddAynesilApi(builder.Configuration);

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    // Gerçek istemci IP'si: hız sınırı, etkinlik günlüğü ve giriş kayıtları RemoteIpAddress'e bakar,
    // bu yüzden en başta. Yalnızca ForwardedHeaders:KnownProxies/KnownNetworks'ten gelen başlıklar işlenir.
    app.UseForwardedHeaders();

    // Serilog's RequestLoggingMiddleware is registered OUTSIDE
    // ExceptionMiddleware so it logs the final HTTP status (401/404/422/…)
    // written by the exception handler, instead of seeing the raw exception
    // first and logging a misleading [ERR] 500 for every expected business
    // outcome (e.g. invalid login credentials, FluentValidation failures,
    // KeyNotFoundException on sub-resources).
    //
    // Genuine 500s are still logged with stack traces by ExceptionMiddleware
    // itself (`_logger.LogError(exception, …)` in its 500 branch), which pipes
    // into Serilog via `ILogger<ExceptionMiddleware>`.
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Dil: Accept-Language → JWT locale → tr. İlk kayıt ExceptionMiddleware'den önce
    // (auth öncesi hatalar da yerelleşir); ikincisi kimlik doğrulamadan sonra claim'i görür.
    app.UseMiddleware<RequestCultureMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title = "AyNesil API";
            opts.Theme = ScalarTheme.Purple;
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AynesilCors");
    app.UseAuthentication();
    app.UseMiddleware<RequestCultureMiddleware>();
    // Parolasını değiştirmesi zorunlu hesap (JWT "mcp") yalnızca parola değiştirme/çıkış/yenileme uçlarını kullanabilir.
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();
    app.UseMiddleware<StaleTenantMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<ActivityLoggingMiddleware>();

    app.MapControllers(); // HealthController GET /health dahil

    Log.Information("AyNesil API starting on {Env}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AyNesil API failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// WebApplicationFactory<Program> (entegrasyon testleri) için erişilebilir giriş noktası.
public partial class Program;
