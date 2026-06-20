using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILeadRepository"/>.
/// crm.lead carries corporation_id, so PostgreSQL RLS (tenant_isolation policy) restricts
/// rows to the current tenant context set by the TenantConnectionInterceptor GUC.
/// The EF Core global query filter (deleted_at IS NULL) provides a secondary application-level guard.
/// </summary>
internal sealed class LeadRepository : GenericRepository<Lead>, ILeadRepository
{
    public LeadRepository(AynesilDbContext context) : base(context) { }

    public async Task<Lead?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(l => l.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .Include(l => l.Activities.OrderByDescending(a => a.OccurredAt))
            .Include(l => l.Interviews.OrderByDescending(i => i.ScheduledAt))
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<Lead>> GetByPipelineStageAsync(
        Guid corporationId,
        Guid pipelineStageId,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = Set
            .AsNoTracking()
            .Where(l => l.CorporationId == corporationId
                     && l.PipelineStageId == pipelineStageId);

        if (campusId.HasValue)
            query = query.Where(l => l.CampusId == campusId.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LeadActivity>> GetActivitiesAsync(
        Guid leadId,
        CancellationToken ct = default)
        => await Context.Set<LeadActivity>()
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LeadActivity>> GetFollowUpsDueAsync(
        Guid corporationId,
        DateTimeOffset dueBy,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        // Join to lead only when campus filter is active to avoid unnecessary join
        if (campusId.HasValue)
        {
            return await (
                from a in Context.Set<LeadActivity>()
                join l in Set on a.LeadId equals l.Id
                where a.CorporationId == corporationId
                   && a.FollowUpAt != null
                   && a.FollowUpAt <= dueBy
                   && l.CampusId == campusId.Value
                orderby a.FollowUpAt
                select a
            ).AsNoTracking().ToListAsync(ct);
        }

        return await Context.Set<LeadActivity>()
            .AsNoTracking()
            .Where(a => a.CorporationId == corporationId
                     && a.FollowUpAt != null
                     && a.FollowUpAt <= dueBy)
            .OrderBy(a => a.FollowUpAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Interview>> GetInterviewsAsync(
        Guid leadId,
        CancellationToken ct = default)
        => await Context.Set<Interview>()
            .AsNoTracking()
            .Where(i => i.LeadId == leadId)
            .OrderByDescending(i => i.ScheduledAt)
            .ToListAsync(ct);

    public async Task<int> CountByStatusAsync(
        Guid corporationId,
        Guid statusId,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = Set.Where(l => l.CorporationId == corporationId && l.StatusId == statusId);
        if (campusId.HasValue) query = query.Where(l => l.CampusId == campusId.Value);
        return await query.CountAsync(ct);
    }

    public async Task<int> CountConvertedAsync(
        Guid corporationId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = Set.Where(l =>
            l.CorporationId == corporationId &&
            l.ConvertedStudentId != null &&
            l.ConvertedAt >= from &&
            l.ConvertedAt <= to);

        if (campusId.HasValue) query = query.Where(l => l.CampusId == campusId.Value);
        return await query.CountAsync(ct);
    }
}
