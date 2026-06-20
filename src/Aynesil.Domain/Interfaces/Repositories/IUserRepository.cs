using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="UserAccount"/> aggregate root.
/// All queries run within the active tenant RLS context (corporation_id GUC).
/// Supplement for operations that benefit from a typed contract over raw IAppDbContext queries.
/// </summary>
public interface IUserRepository : IRepository<UserAccount>
{
    /// <summary>Returns the user identified by username within a corporation. Case-insensitive.</summary>
    Task<UserAccount?> GetByUsernameAsync(Guid corporationId, string username, CancellationToken ct = default);

    /// <summary>Returns the user identified by email within a corporation. Case-insensitive.</summary>
    Task<UserAccount?> GetByEmailAsync(Guid corporationId, string email, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the username is already taken within the corporation.
    /// Pass <paramref name="excludeId"/> to allow the current user to keep their own username during updates.
    /// </summary>
    Task<bool> IsUsernameTakenAsync(Guid corporationId, string username, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the email address is already taken within the corporation.
    /// Pass <paramref name="excludeId"/> to allow the current user to keep their own email during updates.
    /// </summary>
    Task<bool> IsEmailTakenAsync(Guid corporationId, string email, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Returns users with the given status within a corporation.</summary>
    Task<IReadOnlyList<UserAccount>> GetByStatusAsync(Guid corporationId, string status, CancellationToken ct = default);

    /// <summary>Returns the user with their role grants loaded.</summary>
    Task<UserAccount?> GetWithRolesAsync(Guid userId, CancellationToken ct = default);
}
