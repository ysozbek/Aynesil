using System.Data.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Shared.Constants;
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
///   app.current_corporation_id  → used by core.current_corporation_id() in RLS policies (session-level)
///   app.current_user_id         → used by core.current_user_id() and audit_trigger() (session-level)
///   app.care_team_bypass        → used by students.user_can_access_student() in ABAC Phase 3 RLS (SET LOCAL)
///
/// app.care_team_bypass is derived ONLY from the verified JWT 'perm' claims — never from
/// request headers, query parameters, or any client-supplied input.
/// It is set with is_local=true (SET LOCAL / transaction-scoped) so it is automatically
/// reset at the end of each transaction and cannot leak across requests on the same
/// physical connection.
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
        var userId        = _currentUserService.UserId?.ToString() ?? string.Empty;

        // Derive bypass flag exclusively from verified JWT 'perm' claims.
        // 'true' only when the authenticated user holds care_team:bypass permission.
        // Never derive from request headers, query params, or any client input.
        var careTeamBypass = _currentUserService.HasPermission(Permissions.CareTeam.Bypass)
            ? "true"
            : "false";

        try
        {
            await using var cmd = connection.CreateCommand();

            // app.current_corporation_id / app.current_user_id: session-level (is_local=false)
            //   — re-set on every logical connection open (per-request DbContext lifetime).
            // app.care_team_bypass: transaction-local (is_local=true / SET LOCAL)
            //   — resets at end of every transaction; cannot leak across requests on
            //     the same physical pooled connection.
            cmd.CommandText =
                "SELECT set_config('app.current_corporation_id', @corp_id, false), " +
                "       set_config('app.current_user_id',        @user_id,  false), " +
                "       set_config('app.care_team_bypass',        @bypass,   true)";

            var npgsqlCmd = (NpgsqlCommand)cmd;
            npgsqlCmd.Parameters.AddWithValue("corp_id", corporationId);
            npgsqlCmd.Parameters.AddWithValue("user_id", userId);
            npgsqlCmd.Parameters.AddWithValue("bypass",  careTeamBypass);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to set tenant GUCs. corp={CorporationId} user={UserId} bypass={Bypass}",
                corporationId, userId, careTeamBypass);
            throw;
        }
    }
}
