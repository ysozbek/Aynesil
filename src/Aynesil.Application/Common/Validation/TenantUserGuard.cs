using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// Validates a user id chosen by the client (e.g. who conducted an interview) against the
/// current tenant. Composite FKs (#86, V39) enforce the same rule in the database; this check
/// gives a clear 404 before the write instead of a constraint violation.
/// </summary>
public static class TenantUserGuard
{
    public static async Task EnsureBelongsToTenantAsync(
        IAppDbContext db, Guid corporationId, Guid? userId, string field, CancellationToken ct)
    {
        if (!userId.HasValue) return;

        var exists = await db.UserAccounts.AsNoTracking()
            .AnyAsync(u => u.Id == userId.Value
                        && u.CorporationId == corporationId
                        && u.DeletedAt == null, ct);
        if (!exists)
            throw new KeyNotFoundException($"{field}: user {userId} not found in this corporation.");
    }
}
