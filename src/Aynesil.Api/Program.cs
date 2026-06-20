using Aynesil.Api.Authorization;
using Aynesil.Api.Middleware;
using Aynesil.Api.Services;
using Aynesil.Application;
using Aynesil.Application.Common.Interfaces;
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

    // ── Application & Infrastructure layers ──────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── HTTP Context services (per-request identity & tenant context) ─────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<ITenantContext, TenantContextService>();

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration missing.");
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    // ── Permission-based Authorization ────────────────────────────────────────
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    builder.Services.AddAuthorizationBuilder()
        .SetDefaultPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    // ── API controllers & OpenAPI ─────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(opts =>
        opts.AddPolicy("AynesilCors", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

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
