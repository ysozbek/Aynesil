using Aynesil.Domain.Modules.Media.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for media.viewing_authorization.
/// </summary>
public interface IViewingAuthorizationRepository : IRepository<ViewingAuthorization>
{
    /// <summary>
    /// Returns the most recent non-revoked authorization that is currently valid
    /// for the given guardian/student/session combination.
    /// </summary>
    Task<ViewingAuthorization?> GetActiveForGuardianAsync(
        Guid guardianId,
        Guid studentId,
        Guid? sessionId,
        DateTimeOffset now,
        CancellationToken ct = default);
}
