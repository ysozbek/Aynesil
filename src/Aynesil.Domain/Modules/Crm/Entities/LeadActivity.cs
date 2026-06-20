using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Crm.Events;

namespace Aynesil.Domain.Modules.Crm.Entities;

/// <summary>
/// Communication / activity log entry for a lead (call, email, SMS, note, visit).
/// Maps to crm.lead_activity. Append-only — no soft delete, no row_version.
/// The 'direction' column is validated by a DB check constraint ('inbound' | 'outbound').
/// </summary>
public class LeadActivity : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid LeadId { get; private set; }

    /// <summary>Activity kind — ref_type 'activity_type' (call/email/sms/note/visit).</summary>
    public Guid? ActivityTypeId { get; private set; }

    public string? Subject { get; private set; }
    public string? Body { get; private set; }

    /// <summary>'inbound' or 'outbound'. Null for notes/visits.</summary>
    public string? Direction { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>When a follow-up action is scheduled. Drives the follow-up dashboard.</summary>
    public DateTimeOffset? FollowUpAt { get; private set; }

    /// <summary>FK to iam.user_account — the staff member who performed the activity.</summary>
    public Guid? PerformedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────

    public Lead? Lead { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static LeadActivity Create(
        Guid corporationId,
        Guid leadId,
        Guid? activityTypeId,
        string? subject,
        string? body,
        string? direction,
        DateTimeOffset? occurredAt = null,
        DateTimeOffset? followUpAt = null,
        Guid? performedBy = null)
    {
        var activity = new LeadActivity
        {
            CorporationId = corporationId,
            LeadId = leadId,
            ActivityTypeId = activityTypeId,
            Subject = subject,
            Body = body,
            Direction = direction,
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
            FollowUpAt = followUpAt,
            PerformedBy = performedBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        activity.AddDomainEvent(new LeadActivityLoggedEvent(
            activity.Id, corporationId, leadId, activityTypeId, followUpAt));
        return activity;
    }
}
