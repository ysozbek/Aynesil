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
    /// Returns true when the current user has an active care-team assignment
    /// for the specified student.
    ///
    /// Returns true (allow) when:
    ///   - The user is linked to an educator with an active, effective assignment for the student.
    ///
    /// Returns false (early exit → empty list) when:
    ///   - The user is unauthenticated (UserId is null).
    ///   - No educator profile exists for this user.
    ///   - No active assignment links this educator to the student.
    /// </summary>
    public static async Task<bool> CanAccessStudentAsync(
        IAppDbContext db,
        ICurrentUserService currentUser,
        Guid studentId,
        CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
            return false;

        var userId = currentUser.UserId.Value;
        var now    = DateOnly.FromDateTime(DateTime.UtcNow);

        return await db.StudentCareAssignments
            .AsNoTracking()
            .AnyAsync(a =>
                a.StudentId  == studentId &&
                a.Status     == "active"  &&
                a.DeletedAt  == null      &&
                a.ActiveFrom <= now       &&
                (a.ActiveTo  == null || a.ActiveTo > now) &&
                db.Educators.Any(e => e.Id == a.EducatorId && e.UserId == userId),
            ct);
    }
}
