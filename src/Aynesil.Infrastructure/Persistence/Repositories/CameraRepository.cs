using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICameraRepository"/>.
/// All queries run within the active RLS tenant context and respect the soft-delete filter.
/// </summary>
internal sealed class CameraRepository : GenericRepository<Camera>, ICameraRepository
{
    public CameraRepository(AynesilDbContext context) : base(context) { }

    public async Task<Camera?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<bool> CodeExistsAsync(
        Guid corporationId,
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default)
        => await Set
            .AnyAsync(c =>
                c.CorporationId == corporationId
                && c.Code == code
                && (excludeId == null || c.Id != excludeId.Value), ct);
}
