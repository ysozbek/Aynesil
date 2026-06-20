using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMenuRepository"/>.
/// iam.menu_item has a nullable corporation_id:
///   - NULL  → platform default item (visible to all tenants, cannot be deleted)
///   - set   → tenant-scoped custom item
/// All queries union platform defaults with tenant-specific items to produce the
/// complete visible set for a given corporation.
/// </summary>
internal sealed class MenuRepository : GenericRepository<MenuItem>, IMenuRepository
{
    public MenuRepository(AynesilDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MenuItem>> GetActiveFlatListAsync(
        Guid corporationId,
        CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .Where(m => m.IsActive &&
                        (m.CorporationId == null || m.CorporationId == corporationId))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Code)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<MenuItem?> GetWithTranslationsAsync(
        Guid id,
        CancellationToken ct = default) =>
        await Set
            .Include(m => m.Translations)
            .Include(m => m.RequiredPermission)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
}
