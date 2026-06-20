using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICorporationRepository"/>.
/// core.corporation does not carry corporation_id, so RLS does not filter these rows;
/// every query returns all non-deleted corporations visible to the connected DB role.
/// </summary>
internal sealed class CorporationRepository : GenericRepository<Corporation>, ICorporationRepository
{
    public CorporationRepository(AynesilDbContext context) : base(context) { }

    public async Task<Corporation?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code.ToLowerInvariant(), ct);

    public async Task<bool> IsCodeTakenAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = code.ToLowerInvariant();
        return await Set.AnyAsync(c =>
            c.Code == normalized &&
            (excludeId == null || c.Id != excludeId), ct);
    }

    public async Task<int> GetCampusCountAsync(Guid corporationId, CancellationToken ct = default) =>
        await Context.Campuses
            .AsNoTracking()
            .CountAsync(c => c.CorporationId == corporationId, ct);
}
