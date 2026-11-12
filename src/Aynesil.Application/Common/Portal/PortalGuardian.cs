using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.Portal;

/// <summary>
/// Resolves the guardian identity for requests that can be made both by portal guardians and by
/// staff. A guardian's identity is always derived from the signed-in user (students.guardian.user_id),
/// never from the client payload.
/// </summary>
public static class PortalGuardian
{
    /// <summary>
    /// The guardian record linked to the signed-in user in this tenant, or null when the caller
    /// is not a guardian (e.g. staff).
    /// </summary>
    public static async Task<Guid?> FindCurrentGuardianIdAsync(
        IAppDbContext db, Guid corporationId, Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return null;

        return await db.Guardians
            .AsNoTracking()
            .Where(g => g.CorporationId == corporationId && g.UserId == userId.Value)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Effective guardian for a request:
    ///   - caller is a guardian → the caller's own guardian id (a different requested id is rejected);
    ///   - caller is staff      → the requested id (entered on the guardian's behalf), which must
    ///                            belong to the current tenant.
    /// Returns (GuardianId, IsGuardianCaller).
    /// </summary>
    public static async Task<(Guid? GuardianId, bool IsGuardianCaller)> ResolveAsync(
        IAppDbContext db, Guid corporationId, Guid? userId, Guid? requestedGuardianId, CancellationToken ct)
    {
        var own = await FindCurrentGuardianIdAsync(db, corporationId, userId, ct);
        if (own.HasValue)
        {
            if (requestedGuardianId.HasValue && requestedGuardianId != own)
                throw new ForbiddenAccessException("Guardians can only act on their own behalf.");
            return (own, true);
        }

        if (requestedGuardianId.HasValue)
        {
            var exists = await db.Guardians.AsNoTracking()
                .AnyAsync(g => g.CorporationId == corporationId && g.Id == requestedGuardianId.Value, ct);
            if (!exists)
                throw new KeyNotFoundException($"Guardian {requestedGuardianId} not found.");
        }

        return (requestedGuardianId, false);
    }

    /// <summary>Throws when the guardian has no active portal access to the student.</summary>
    public static async Task EnsureCanAccessStudentAsync(
        IAppDbContext db, Guid corporationId, Guid guardianId, Guid studentId,
        CancellationToken ct, bool requireCamera = false)
    {
        var ok = await db.GuardianPortalAccesses
            .AsNoTracking()
            .AnyAsync(gpa => gpa.CorporationId == corporationId
                          && gpa.GuardianId    == guardianId
                          && gpa.StudentId     == studentId
                          && gpa.RevokedAt     == null
                          && (!requireCamera || gpa.CanViewCamera), ct);
        if (!ok)
            throw new ForbiddenAccessException("The signed-in guardian has no active access to this student.");
    }
}
