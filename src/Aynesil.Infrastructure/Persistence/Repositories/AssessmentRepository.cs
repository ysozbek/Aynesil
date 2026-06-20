using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAssessmentRepository"/>.
/// All queries execute within the active tenant RLS context (corporation_id GUC set by
/// TenantConnectionInterceptor) and respect the soft-delete query filter on AssessmentSession.
/// Templates are NOT soft-deleted (they use is_active), so template queries bypass the
/// soft-delete filter and apply the is_active flag explicitly.
/// </summary>
internal sealed class AssessmentRepository
    : GenericRepository<AssessmentSession>, IAssessmentRepository
{
    public AssessmentRepository(AynesilDbContext context) : base(context) { }

    // ── Session ───────────────────────────────────────────────────────────────

    public async Task<AssessmentSession?> GetSessionWithDetailsAsync(
        Guid sessionId, CancellationToken ct = default)
        => await Set
            .Include(s => s.Responses)
                .ThenInclude(r => r.Item)
                    .ThenInclude(i => i.Section)
            .Include(s => s.Recommendations)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<IReadOnlyList<AssessmentSession>> GetSessionsByLeadAsync(
        Guid leadId, CancellationToken ct = default)
        => await Set
            .AsNoTracking()
            .Where(s => s.LeadId == leadId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssessmentSession>> GetSessionsByStudentAsync(
        Guid studentId, CancellationToken ct = default)
        => await Set
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssessmentSession>> GetSessionsByDateRangeAsync(
        Guid corporationId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? campusId = null,
        CancellationToken ct = default)
    {
        var query = Set
            .AsNoTracking()
            .Where(s => s.CorporationId == corporationId
                     && s.ScheduledAt >= from
                     && s.ScheduledAt <= to);

        if (campusId.HasValue)
            query = query.Where(s => s.CampusId == campusId.Value);

        return await query
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync(ct);
    }

    // ── Template ──────────────────────────────────────────────────────────────

    public async Task<AssessmentTemplate?> GetTemplateWithSectionsAsync(
        Guid templateId, CancellationToken ct = default)
        => await Context.Set<AssessmentTemplate>()
            .Include(t => t.Translations)
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

    public async Task<IReadOnlyList<AssessmentTemplate>> GetActiveTemplatesAsync(
        Guid corporationId, CancellationToken ct = default)
        => await Context.Set<AssessmentTemplate>()
            .AsNoTracking()
            .Where(t => t.IsActive
                     && (t.CorporationId == null || t.CorporationId == corporationId))
            .OrderBy(t => t.Code)
            .ThenByDescending(t => t.Version)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssessmentTemplate>> GetTemplateVersionsAsync(
        string code, Guid? corporationId = null, CancellationToken ct = default)
        => await Context.Set<AssessmentTemplate>()
            .AsNoTracking()
            .Where(t => t.Code == code
                     && (corporationId == null
                         ? t.CorporationId == null
                         : t.CorporationId == corporationId))
            .OrderByDescending(t => t.Version)
            .ToListAsync(ct);

    // ── Report ────────────────────────────────────────────────────────────────

    public async Task<AssessmentReport?> GetReportBySessionAsync(
        Guid sessionId, CancellationToken ct = default)
        => await Context.Set<AssessmentReport>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId, ct);

    // ── Program Recommendation ────────────────────────────────────────────────

    public async Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsBySessionAsync(
        Guid sessionId, CancellationToken ct = default)
        => await Context.Set<ProgramRecommendation>()
            .AsNoTracking()
            .Where(r => r.AssessmentSessionId == sessionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsByLeadAsync(
        Guid leadId, CancellationToken ct = default)
        => await Context.Set<ProgramRecommendation>()
            .AsNoTracking()
            .Where(r => r.LeadId == leadId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsByStudentAsync(
        Guid studentId, CancellationToken ct = default)
        => await Context.Set<ProgramRecommendation>()
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
