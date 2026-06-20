using Aynesil.Domain.Modules.Assessment.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Domain repository contract for the Assessment aggregate.
/// The generic IRepository&lt;AssessmentSession&gt; base covers basic CRUD against sessions.
/// The methods below cover the read patterns required by assessment queries and
/// the workflow operations that span multiple aggregate sub-entities.
/// All queries execute within the active RLS tenant context.
/// </summary>
public interface IAssessmentRepository : IRepository<AssessmentSession>
{
    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>Returns a session with its responses and report eagerly loaded.</summary>
    Task<AssessmentSession?> GetSessionWithDetailsAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Returns all non-deleted sessions for the given lead, newest first.</summary>
    Task<IReadOnlyList<AssessmentSession>> GetSessionsByLeadAsync(
        Guid leadId, CancellationToken ct = default);

    /// <summary>Returns all non-deleted sessions for the given student, newest first.</summary>
    Task<IReadOnlyList<AssessmentSession>> GetSessionsByStudentAsync(
        Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Returns sessions whose scheduled_at falls in the given range, scoped to a corporation.
    /// Used by calendar and scheduling reports.
    /// </summary>
    Task<IReadOnlyList<AssessmentSession>> GetSessionsByDateRangeAsync(
        Guid corporationId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? campusId = null,
        CancellationToken ct = default);

    // ── Template ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a template with its sections and items eagerly loaded.
    /// Returns null when not found.
    /// </summary>
    Task<AssessmentTemplate?> GetTemplateWithSectionsAsync(
        Guid templateId, CancellationToken ct = default);

    /// <summary>
    /// Returns active templates visible to the given corporation:
    /// platform-provided templates (corporation_id IS NULL) plus the
    /// tenant's own templates.
    /// </summary>
    Task<IReadOnlyList<AssessmentTemplate>> GetActiveTemplatesAsync(
        Guid corporationId, CancellationToken ct = default);

    /// <summary>
    /// Returns all versions of the template identified by the given code,
    /// scoped to the given corporation or platform-wide if corporation_id = NULL.
    /// Used by the versioning history view.
    /// </summary>
    Task<IReadOnlyList<AssessmentTemplate>> GetTemplateVersionsAsync(
        string code, Guid? corporationId = null, CancellationToken ct = default);

    // ── Report ────────────────────────────────────────────────────────────────

    /// <summary>Returns the report for the given session, or null if none exists yet.</summary>
    Task<AssessmentReport?> GetReportBySessionAsync(
        Guid sessionId, CancellationToken ct = default);

    // ── Program Recommendation ─────────────────────────────────────────────

    /// <summary>Returns all recommendations linked to the given session.</summary>
    Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsBySessionAsync(
        Guid sessionId, CancellationToken ct = default);

    /// <summary>Returns all recommendations linked to the given lead.</summary>
    Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsByLeadAsync(
        Guid leadId, CancellationToken ct = default);

    /// <summary>Returns all recommendations linked to the given student.</summary>
    Task<IReadOnlyList<ProgramRecommendation>> GetRecommendationsByStudentAsync(
        Guid studentId, CancellationToken ct = default);
}
