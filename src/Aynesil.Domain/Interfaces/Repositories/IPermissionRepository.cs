using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="Permission"/>.
/// Permissions are platform-global (no corporation_id). RLS does not filter them.
/// Tenants can read permissions to assign them to roles but cannot create or delete permissions.
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>Returns the permission identified by its stable code string.</summary>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns all permissions for a given resource (e.g. 'student', 'session').</summary>
    Task<IReadOnlyList<Permission>> GetByResourceAsync(string resource, CancellationToken ct = default);

    /// <summary>Returns all permissions currently assigned to a role.</summary>
    Task<IReadOnlyList<Permission>> GetByRoleAsync(Guid roleId, CancellationToken ct = default);
}
