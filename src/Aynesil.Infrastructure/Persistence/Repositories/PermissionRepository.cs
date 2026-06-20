using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPermissionRepository"/>.
/// iam.permission is platform-global (no corporation_id). RLS does not restrict it.
/// No soft-delete on permissions — they are never removed, only potentially deprecated.
/// </summary>
internal sealed class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(AynesilDbContext context) : base(context) { }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<Permission>> GetByResourceAsync(
        string resource, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .Where(p => p.Resource == resource.ToLowerInvariant())
            .OrderBy(p => p.Action)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByRoleAsync(
        Guid roleId, CancellationToken ct = default) =>
        await Context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission!)
            .OrderBy(p => p.Resource).ThenBy(p => p.Action)
            .ToListAsync(ct);
}
