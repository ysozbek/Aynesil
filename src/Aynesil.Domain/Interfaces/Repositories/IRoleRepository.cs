using Aynesil.Domain.Modules.Iam.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Typed repository for <see cref="Role"/>.
/// Roles may be tenant-scoped (CorporationId set) or system-level templates (CorporationId null).
/// Tenant-scoped roles are filtered by the active RLS context.
/// System roles (corporation_id IS NULL) are always visible to all tenants.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>Returns the role identified by code within a corporation (or system scope when corporationId is null).</summary>
    Task<Role?> GetByCodeAsync(Guid? corporationId, string code, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the code is already taken within the given scope.
    /// Pass <paramref name="excludeId"/> to allow the current role to keep its own code during updates.
    /// </summary>
    Task<bool> IsCodeTakenAsync(Guid? corporationId, string code, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Returns all roles visible to a tenant, optionally including system templates.</summary>
    Task<IReadOnlyList<Role>> GetByTenantAsync(Guid corporationId, bool includeSystem = true, CancellationToken ct = default);

    /// <summary>Returns the role with its permission grants eagerly loaded.</summary>
    Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken ct = default);
}
