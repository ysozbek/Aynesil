using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IViewingAuthorizationRepository"/>.
/// Queries use the partial index ix_viewing_auth_guardian (is_revoked = false).
/// </summary>
internal sealed class ViewingAuthorizationRepository
    : GenericRepository<ViewingAuthorization>, IViewingAuthorizationRepository
{
    public ViewingAuthorizationRepository(AynesilDbContext context) : base(context) { }

    public async Task<ViewingAuthorization?> GetActiveForGuardianAsync(
        Guid guardianId,
        Guid studentId,
        Guid? sessionId,
        DateTimeOffset now,
        CancellationToken ct = default)
        => await Set
            .Where(a =>
                a.GuardianId == guardianId
                && a.StudentId == studentId
                && !a.IsRevoked
                && a.ValidFrom <= now
                && now < a.ValidTo
                && (sessionId == null || a.SessionId == null || a.SessionId == sessionId))
            .OrderByDescending(a => a.ValidTo)
            .FirstOrDefaultAsync(ct);
}
