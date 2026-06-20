using System.Data.Common;
using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Aynesil.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets the PostgreSQL session GUCs required for Row-Level Security on every connection.
/// The application connects as the least-privilege role (aynesil_app) which is subject
/// to RLS policies. The owner role bypasses RLS and is used ONLY for migrations/seeding.
///
/// GUCs set:
///   app.current_corporation_id  → used by core.current_corporation_id() in RLS policies
///   app.current_user_id         → used by core.current_user_id() and audit_trigger()
///
/// When CorporationId is null (unauthenticated/system request), an empty string is set
/// so that core.current_corporation_id() returns NULL and RLS returns zero rows (default-deny).
/// </summary>
public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TenantConnectionInterceptor> _logger;

    public TenantConnectionInterceptor(
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ILogger<TenantConnectionInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync(connection, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContextAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task SetTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection) return;

        var corporationId = _tenantContext.CorporationId?.ToString() ?? string.Empty;
        var userId = _currentUserService.UserId?.ToString() ?? string.Empty;

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT set_config('app.current_corporation_id', @corp_id, false), " +
                "       set_config('app.current_user_id',        @user_id,  false)";

            var corpParam = ((NpgsqlCommand)cmd).Parameters.AddWithValue("corp_id", corporationId);
            var userParam = ((NpgsqlCommand)cmd).Parameters.AddWithValue("user_id", userId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to set tenant GUCs. corp={CorporationId} user={UserId}",
                corporationId, userId);
            throw;
        }
    }
}
