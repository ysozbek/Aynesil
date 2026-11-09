using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Users;

/// <summary>
/// Usernames are unique across ALL corporations (V36), because login resolves
/// users by username alone. The app role cannot see other tenants' rows under
/// RLS, so the check goes through the SECURITY DEFINER iam.username_exists().
/// </summary>
public static class UsernameAvailability
{
    public static async Task<bool> UsernameExistsAsync(
        this IAppDbContext db, string username, CancellationToken ct)
    {
        return await db.Database
            .SqlQueryRaw<bool>(
                @"SELECT iam.username_exists({0}::citext) AS ""Value""", username)
            .SingleAsync(ct);
    }
}
