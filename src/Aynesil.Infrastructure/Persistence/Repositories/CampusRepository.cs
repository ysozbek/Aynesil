using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICampusRepository"/>.
/// core.campus carries corporation_id, so PostgreSQL RLS (tenant_isolation policy) restricts
/// rows to the current tenant context set by the TenantConnectionInterceptor GUC.
/// The EF Core global query filter (deleted_at IS NULL) provides an application-level guard.
/// </summary>
internal sealed class CampusRepository : GenericRepository<Campus>, ICampusRepository
{
    public CampusRepository(AynesilDbContext context) : base(context) { }

    public async Task<Campus?> GetByCodeAsync(Guid corporationId, string code, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .Include(c => c.Corporation)
            .FirstOrDefaultAsync(c =>
                c.CorporationId == corporationId &&
                c.Code == code.ToUpperInvariant(), ct);

    public async Task<bool> IsCodeTakenAsync(
        Guid corporationId, string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = code.ToUpperInvariant();
        return await Set.AnyAsync(c =>
            c.CorporationId == corporationId &&
            c.Code == normalized &&
            (excludeId == null || c.Id != excludeId), ct);
    }

    public async Task<IReadOnlyList<Campus>> GetByCorporationAsync(
        Guid corporationId, bool? isActive = null, CancellationToken ct = default)
    {
        var query = Set
            .AsNoTracking()
            .Where(c => c.CorporationId == corporationId);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }
}
