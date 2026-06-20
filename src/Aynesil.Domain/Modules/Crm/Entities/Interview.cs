using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Crm.Events;

namespace Aynesil.Domain.Modules.Crm.Entities;

/// <summary>
/// Pre-enrollment interview for a lead prospect.
/// Maps to crm.interview. Transitions are guarded by invariant methods.
/// Status values are validated by a DB check constraint:
///   'scheduled' | 'completed' | 'no_show' | 'cancelled'.
/// Note: this table does not have created_by/updated_by or deleted_at columns,
/// so it inherits BaseEntity directly and declares its own audit subset.
/// </summary>
public class Interview : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid? CampusId { get; private set; }

    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? ConductedAt { get; private set; }

    /// <summary>FK to iam.user_account — the interviewer.</summary>
    public Guid? ConductedBy { get; private set; }

    public string? Outcome { get; private set; }
    public string? Recommendation { get; private set; }

    /// <summary>
    /// Lifecycle status: scheduled | completed | no_show | cancelled.
    /// Enforced by the DB check constraint and the domain methods below.
    /// </summary>
    public string Status { get; private set; } = InterviewStatus.Scheduled;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    // ── Navigation ────────────────────────────────────────────────────────────

    public Lead? Lead { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Interview Schedule(
        Guid corporationId,
        Guid leadId,
        Guid? campusId,
        DateTimeOffset? scheduledAt)
    {
        var interview = new Interview
        {
            CorporationId = corporationId,
            LeadId = leadId,
            CampusId = campusId,
            ScheduledAt = scheduledAt,
            Status = InterviewStatus.Scheduled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        interview.AddDomainEvent(new InterviewScheduledEvent(interview.Id, corporationId, leadId, scheduledAt));
        return interview;
    }

    // ── Lifecycle transitions ─────────────────────────────────────────────────

    public void Complete(string? outcome, string? recommendation, Guid? conductedBy = null)
    {
        if (Status == InterviewStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled interview.");

        Status = InterviewStatus.Completed;
        ConductedAt = DateTimeOffset.UtcNow;
        ConductedBy = conductedBy;
        Outcome = outcome;
        Recommendation = recommendation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow()
    {
        if (Status is InterviewStatus.Completed or InterviewStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot mark as no-show when interview status is '{Status}'.");

        Status = InterviewStatus.NoShow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == InterviewStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed interview.");

        Status = InterviewStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reschedule(DateTimeOffset newScheduledAt, Guid? updatedBy = null)
    {
        if (Status is InterviewStatus.Completed or InterviewStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot reschedule an interview with status '{Status}'.");

        ScheduledAt = newScheduledAt;
        Status = InterviewStatus.Scheduled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(
        Guid? campusId,
        DateTimeOffset? scheduledAt,
        string? outcome,
        string? recommendation)
    {
        CampusId = campusId;
        ScheduledAt = scheduledAt;
        Outcome = outcome;
        Recommendation = recommendation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Allowed values for Interview.Status (mirrored from the DB check constraint).</summary>
public static class InterviewStatus
{
    public const string Scheduled = "scheduled";
    public const string Completed = "completed";
    public const string NoShow = "no_show";
    public const string Cancelled = "cancelled";
}
