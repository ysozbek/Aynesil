using Aynesil.Api.Authorization;
using Aynesil.Api.Services;
using Aynesil.Application;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using Aynesil.Infrastructure;
using Aynesil.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Aynesil.Api;

/// <summary>
/// API'nin tüm servis kayıtları. Program.cs ve DI doğrulama testi (ValidateOnBuild + ValidateScopes)
/// aynı kayıt kümesini kullanır; böylece eksik/yanlış ömürlü kayıt Development'ta ve testte hemen görünür.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddAynesilApi(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Application & Infrastructure layers ──────────────────────────────────
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // ── HTTP Context services (per-request identity & tenant context) ─────────
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContextService>();

        // ── JWT Authentication ────────────────────────────────────────────────────
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration missing.");
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        // PermissionPolicyProvider: [HasPermission("x:y")] → transient policy with PermissionRequirement
        // PermissionAuthorizationHandler: evaluates PermissionRequirement against JWT 'perm' claims
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        // ── API controllers & OpenAPI ─────────────────────────────────────────────
        services.AddControllers()
            // JSON çözümleyicinin ham hata metni (iç tip adı, namespace, satır/konum) istemciye gitmez (Y8).
            .AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)
            .ConfigureApiBehaviorOptions(opts =>
            {
                // Model bağlama hatalarını (400) ExceptionMiddleware ile aynı
                // { success:false, message, errors } zarfına çevir.
                opts.InvalidModelStateResponseFactory = ctx =>
                {
                    // Kültür RequestCultureMiddleware'den gelir. Model bağlama hatalarının kodu yoktur → Error.InvalidValue.
                    // Çerçevenin ürettiği metinler (JSON çözümleyici, "The X field is required." vb.) İngilizcedir ve
                    // iç tip/yol bilgisi içerebilir; istemciye yalnızca yerelleştirilmiş genel metin gider (alan anahtarı korunur).
                    var invalidValue = Msg.Get(MsgKeys.Error.InvalidValue);
                    var pairs = ctx.ModelState
                        .Where(kv => kv.Value is { Errors.Count: > 0 })
                        .Select(kv => new KeyValuePair<string, string[]>(
                            kv.Key,
                            kv.Value!.Errors.Select(_ => invalidValue).Distinct().ToArray()))
                        .ToList();
                    var errors = Aynesil.Shared.ErrorKeyFormatter.Normalize(pairs);
                    var details = Aynesil.Shared.ErrorKeyFormatter.Normalize(pairs.Select(kv =>
                        new KeyValuePair<string, Aynesil.Shared.ErrorDetail[]>(
                            kv.Key,
                            kv.Value.Select(m => new Aynesil.Shared.ErrorDetail(MsgKeys.Error.InvalidValue.Value, m)).ToArray())));

                    var body = Aynesil.Shared.ApiResponse.Fail(
                            Msg.Get(MsgKeys.Error.InvalidRequest), MsgKeys.Error.InvalidRequest.Value, errors, details)
                        with { TraceId = ctx.HttpContext.TraceIdentifier };

                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(body);
                };
            });
        services.AddOpenApi();

        // ── CORS ──────────────────────────────────────────────────────────────────
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(opts =>
            opts.AddPolicy("AynesilCors", policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

        return services;
    }
}
