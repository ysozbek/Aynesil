using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IEducationPlanRepository"/>.
/// All queries run within the active tenant RLS context and respect the soft-delete filter.
/// </summary>
internal sealed class EducationPlanRepository : GenericRepository<EducationPlan>, IEducationPlanRepository
{
    public EducationPlanRepository(AynesilDbContext context) : base(context) { }

    public async Task<EducationPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(p => p.PlanGoals)
                .ThenInclude(pg => pg.Goal)
            .Include(p => p.Reviews)
            .Include(p => p.Approvals)
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<EducationPlan>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        string? status = null,
        CancellationToken ct = default)
    {
        var q = Set.Where(p => p.CorporationId == corporationId && p.StudentId == studentId);

        if (status is not null)
            q = q.Where(p => p.Status == status);

        return await q.OrderByDescending(p => p.Version).ToListAsync(ct);
    }

    public async Task<EducationPlan?> GetGuardianVisiblePlanAsync(
        Guid corporationId,
        Guid studentId,
        CancellationToken ct = default)
        => await Set
            .Include(p => p.PlanGoals)
                .ThenInclude(pg => pg.Goal)
            .Where(p => p.CorporationId == corporationId
                     && p.StudentId == studentId
                     && p.GuardianVisible
                     && (p.Status == "approved" || p.Status == "active"))
            .OrderByDescending(p => p.ApprovedAt)
            .FirstOrDefaultAsync(ct);
}
