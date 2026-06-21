using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGoalRepository"/>.
/// All queries run within the active tenant RLS context and respect the soft-delete filter.
/// </summary>
internal sealed class GoalRepository : GenericRepository<StudentGoal>, IGoalRepository
{
    public GoalRepository(AynesilDbContext context) : base(context) { }

    public async Task<StudentGoal?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(g => g.ProgressRecords)
            .Include(g => g.ChildGoals)
            .Include(g => g.Template)
                .ThenInclude(t => t!.Translations)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<StudentGoal>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        string? horizon = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var q = Set.Where(g => g.CorporationId == corporationId && g.StudentId == studentId);

        if (horizon is not null)
            q = q.Where(g => g.Horizon == horizon);

        if (status is not null)
            q = q.Where(g => g.Status == status);

        return await q.OrderBy(g => g.Horizon).ThenBy(g => g.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GoalTemplate>> GetTemplatesAsync(
        Guid? corporationId,
        Guid? libraryId = null,
        Guid? categoryId = null,
        Guid? developmentAreaId = null,
        CancellationToken ct = default)
    {
        var q = Context.Set<GoalTemplate>().AsQueryable();

        // Platform-provided (null corp) or tenant-specific
        q = corporationId.HasValue
            ? q.Where(t => t.CorporationId == null || t.CorporationId == corporationId.Value)
            : q.Where(t => t.CorporationId == null);

        if (libraryId.HasValue)
            q = q.Where(t => t.LibraryId == libraryId.Value);

        if (categoryId.HasValue)
            q = q.Where(t => t.CategoryId == categoryId.Value);

        if (developmentAreaId.HasValue)
            q = q.Where(t => t.DevelopmentAreaId == developmentAreaId.Value);

        return await q
            .Include(t => t.Translations)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
    }
}
