using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.CareTeam;

/// <summary>
/// App-layer care-team access helper used by clinical query handlers.
///
/// PURPOSE: UX performance pre-filter — return empty list quickly (before the main DB
/// round-trip) when the user has no clinical access to a student.
///
/// SECURITY: This is NOT the security guarantee. PostgreSQL RESTRICTIVE RLS policies
/// (Phase 3, students.user_can_access_student()) are the actual security backstop.
/// Even if this filter is bypassed or miscalled, RLS ensures no unauthorized data leaks.
///
/// BYPASS: Handlers call HasBypass(user) before CanAccessStudentAsync to short-circuit
/// for privileged users. BypassPermissionCode avoids resolving Permissions.CareTeam.Bypass
/// from Aynesil.Shared.Constants (which collides with the Permissions feature namespace).
/// </summary>
public static class CareTeamFilter
{
    /// <summary>
    /// The care_team:bypass permission code (mirrors Aynesil.Shared.Constants.Permissions.CareTeam.Bypass).
    /// Kept here to avoid namespace collision with the Features/Permissions feature folder
    /// inside the Aynesil.Application project.
    /// </summary>
    public const string BypassPermissionCode = "care_team:bypass";

    /// <summary>Returns true when the user holds care_team:bypass and should skip the care-team filter.</summary>
    public static bool HasBypass(ICurrentUserService currentUser)
        => currentUser.HasPermission(BypassPermissionCode);
    /// <summary>
    /// Same rule as the RESTRICTIVE RLS backstop: delegates to <c>students.user_can_access_student()</c> (V16), so the
    /// pre-filter can never be stricter than the database. Returns true when:
    ///   - the tenant is in RBAC-only mode (<c>core.setting_value care_team_abac_enabled</c> missing/false — the
    ///     default for every tenant until it opts in), or
    ///   - the user is linked to an educator with an active, effective care-team assignment for the student.
    /// Returns false (early exit → empty list) only when the tenant has ABAC enabled and no such assignment exists
    /// (or the user is unauthenticated).
    ///
    /// Before this delegated to the DB, the helper checked the assignment table alone and ignored the per-tenant
    /// switch: in RBAC-only tenants every user without care_team:bypass (e.g. educators) saw empty per-student goal,
    /// progress, plan, assessment and sub-resource lists although RLS allowed the rows.
    /// The function reads app.current_corporation_id / app.current_user_id, set on the connection by
    /// TenantConnectionInterceptor. Callers check <see cref="HasBypass"/> first.
    /// </summary>
    public static async Task<bool> CanAccessStudentAsync(
        IAppDbContext db,
        ICurrentUserService currentUser,
        Guid studentId,
        CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
            return false;

        return await db.Database
            .SqlQueryRaw<bool>(@"SELECT students.user_can_access_student({0}) AS ""Value""", studentId)
            .FirstAsync(ct);
    }
}
