using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Educators.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IEducatorRepository"/>.
/// All queries run within the active tenant RLS context and respect the soft-delete filter.
/// </summary>
internal sealed class EducatorRepository : GenericRepository<Educator>, IEducatorRepository
{
    public EducatorRepository(AynesilDbContext context) : base(context) { }

    public async Task<Educator?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(e => e.Campuses)
            .Include(e => e.Specialties)
            .Include(e => e.Certifications)
            .Include(e => e.Supervisors)
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Educator>> GetByCorporationAsync(
        Guid corporationId,
        Guid? campusId = null,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var q = Set.Where(e => e.CorporationId == corporationId);

        if (activeOnly)
            q = q.Where(e => e.IsActive);

        if (campusId.HasValue)
            q = q.Where(e =>
                e.PrimaryCampusId == campusId.Value ||
                e.Campuses.Any(c => c.CampusId == campusId.Value && c.ActiveTo == null));

        return await q.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Educator>> GetBySpecialtyAsync(
        Guid corporationId,
        Guid specialtyId,
        CancellationToken ct = default)
        => await Set
            .Where(e => e.CorporationId == corporationId
                     && e.Specialties.Any(s => s.SpecialtyId == specialtyId))
            .ToListAsync(ct);
}
