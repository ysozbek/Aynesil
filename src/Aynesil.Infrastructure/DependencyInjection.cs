using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Infrastructure.Events;
using Aynesil.Infrastructure.Options;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Infrastructure.Persistence.Interceptors;
using Aynesil.Infrastructure.Persistence.Repositories;
using Aynesil.Infrastructure.Services;
using Aynesil.Infrastructure.Services.Auth;
using Aynesil.Infrastructure.Services.Cache;
using Aynesil.Infrastructure.Services.Files;
using Aynesil.Infrastructure.Services.Jobs;
using Aynesil.Infrastructure.Services.Localization;
using Aynesil.Infrastructure.Services.Ref;
using Aynesil.Infrastructure.Services.Settings;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Aynesil.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDbContext(configuration)
            .AddCaching(configuration)
            .AddPlatformServices()
            .AddHangfireJobs(configuration);

        return services;
    }

    private static IServiceCollection AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Npgsql 6+: use timestamptz → DateTimeOffset
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

        services.AddScoped<TenantConnectionInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<DomainEventInterceptor>();

        services.AddDbContext<AynesilDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "core");
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
            });

            options.AddInterceptors(
                sp.GetRequiredService<TenantConnectionInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());

#if DEBUG
            options.EnableDetailedErrors().EnableSensitiveDataLogging();
#endif
        });

        // IAppDbContext → AynesilDbContext (aynı scoped instance, iki kez oluşturulmaz)
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AynesilDbContext>());

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConn));

            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConn;
                opts.InstanceName = "aynesil:";
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        return services;
    }

    private static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IRefDataService, RefDataService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IStorageProvider, LocalStorageProvider>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IEventBus, InProcessEventBus>();

        // ── Typed repositories ───────────────────────────────────────────────────
        services.AddScoped<ICorporationRepository, CorporationRepository>();
        services.AddScoped<ICampusRepository, CampusRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IEducatorRepository, EducatorRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IEducationPlanRepository, EducationPlanRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        // ── Media / Camera repositories ──────────────────────────────────────
        services.AddScoped<ICameraRepository, CameraRepository>();
        services.AddScoped<IViewingAuthorizationRepository, ViewingAuthorizationRepository>();
        services.AddScoped<IViewingLogRepository, ViewingLogRepository>();

        return services;
    }

    private static IServiceCollection AddHangfireJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddHangfire(cfg =>
            cfg.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = 5;
            opts.Queues = ["default", "notifications", "reports", "outbox"];
        });

        services.AddScoped<IBackgroundJobService, HangfireJobService>();

        return services;
    }
}
