using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProgramRepository"/>.
/// All queries run within the active tenant RLS context and respect the soft-delete filter.
/// </summary>
internal sealed class ProgramRepository : GenericRepository<EducationProgram>, IProgramRepository
{
    public ProgramRepository(AynesilDbContext context) : base(context) { }

    public async Task<EducationProgram?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(p => p.Services)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<EducationProgram>> GetByCorporationAsync(
        Guid corporationId,
        Guid? programTypeId = null,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var q = Set.Where(p => p.CorporationId == corporationId);

        if (activeOnly)
            q = q.Where(p => p.IsActive);

        if (programTypeId.HasValue)
            q = q.Where(p => p.ProgramTypeId == programTypeId.Value);

        return await q.ToListAsync(ct);
    }

    public async Task<bool> CodeExistsAsync(
        Guid corporationId,
        string code,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var q = Set.Where(p => p.CorporationId == corporationId && p.Code == code);

        if (excludeId.HasValue)
            q = q.Where(p => p.Id != excludeId.Value);

        return await q.AnyAsync(ct);
    }
}
