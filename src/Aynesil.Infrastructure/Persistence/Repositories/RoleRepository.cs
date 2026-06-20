using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRoleRepository"/>.
/// iam.role has a nullable corporation_id:
///   - NULL  → system template (visible to all tenants)
///   - set   → tenant-specific role (filtered by RLS to its corporation)
/// The GetByTenantAsync method explicitly combines both scopes since RLS alone
/// may not union system roles for a given tenant's queries.
/// </summary>
internal sealed class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(AynesilDbContext context) : base(context) { }

    public async Task<Role?> GetByCodeAsync(
        Guid? corporationId, string code, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.CorporationId == corporationId &&
                r.Code == code.ToLowerInvariant(), ct);

    public async Task<bool> IsCodeTakenAsync(
        Guid? corporationId, string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = code.ToLowerInvariant();
        return await Set.AnyAsync(r =>
            r.CorporationId == corporationId &&
            r.Code == normalized &&
            (excludeId == null || r.Id != excludeId), ct);
    }

    public async Task<IReadOnlyList<Role>> GetByTenantAsync(
        Guid corporationId, bool includeSystem = true, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .Where(r => r.CorporationId == corporationId ||
                        (includeSystem && r.CorporationId == null))
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken ct = default) =>
        await Set
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);
}
