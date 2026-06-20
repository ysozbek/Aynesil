using Aynesil.Domain.Modules.Crm.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Domain repository contract for the CRM Lead aggregate.
/// Implementations live in Aynesil.Infrastructure and are resolved via DI.
/// All queries are executed within the active tenant context (RLS applies).
/// </summary>
public interface ILeadRepository : IRepository<Lead>
{
    /// <summary>Returns the lead with its status history, activities, and interviews pre-loaded.</summary>
    Task<Lead?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all leads in a given pipeline stage, optionally filtered by campus.</summary>
    Task<IReadOnlyList<Lead>> GetByPipelineStageAsync(
        Guid corporationId,
        Guid pipelineStageId,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all activities logged against a specific lead, newest first.</summary>
    Task<IReadOnlyList<LeadActivity>> GetActivitiesAsync(
        Guid leadId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns activities that have a follow-up due on or before <paramref name="dueBy"/>.
    /// Used by the follow-up dashboard and reminder jobs.
    /// </summary>
    Task<IReadOnlyList<LeadActivity>> GetFollowUpsDueAsync(
        Guid corporationId,
        DateTimeOffset dueBy,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Returns all interviews scheduled for a specific lead.</summary>
    Task<IReadOnlyList<Interview>> GetInterviewsAsync(
        Guid leadId,
        CancellationToken ct = default);

    /// <summary>Count of leads by status — used by the pipeline dashboard.</summary>
    Task<int> CountByStatusAsync(
        Guid corporationId,
        Guid statusId,
        Guid? campusId = null,
        CancellationToken ct = default);

    /// <summary>Count of leads converted to students within a date range — used for conversion-rate reporting.</summary>
    Task<int> CountConvertedAsync(
        Guid corporationId,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? campusId = null,
        CancellationToken ct = default);
}
