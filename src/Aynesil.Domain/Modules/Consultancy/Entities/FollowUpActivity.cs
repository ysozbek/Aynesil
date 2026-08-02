using Aynesil.Domain.Modules.Consultancy.Events;

namespace Aynesil.Domain.Modules.Consultancy.Entities;

/// <summary>
/// Maps to consultancy.follow_up_activity.
/// An action item arising from a school visit or observation record.
/// At least one of ConsultancyPlanId or SchoolVisitId must be set (enforced at application layer).
/// Status workflow: pending → in_progress → completed (terminal)
///                  pending | in_progress → cancelled (terminal)
/// DB schema has no deleted_at column — lifecycle is managed by status transitions only.
/// </summary>
public class FollowUpActivity : TenantEntity
{
    public Guid? ConsultancyPlanId { get; private set; }
    public Guid? SchoolVisitId { get; private set; }
    public Guid? ObservationRecordId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? DueDate { get; private set; }

    /// <summary>Educator responsible for this activity.</summary>
    public Guid? AssignedTo { get; private set; }

    /// <summary>'pending' | 'in_progress' | 'completed' | 'cancelled'</summary>
    public string Status { get; private set; } = "pending";

    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }

    /// <summary>Completion notes or any additional context added during the activity.</summary>
    public string? Notes { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static FollowUpActivity Create(
        Guid corporationId,
        string title,
        Guid? consultancyPlanId,
        Guid? schoolVisitId,
        Guid? observationRecordId = null,
        string? description = null,
        DateOnly? dueDate = null,
        Guid? assignedTo = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Follow-up activity title is required.", nameof(title));
        if (consultancyPlanId == null && schoolVisitId == null)
            throw new ArgumentException(
                "A follow-up activity must be linked to a consultancy plan or a school visit.");

        var activity = new FollowUpActivity
        {
            CorporationId       = corporationId,
            ConsultancyPlanId   = consultancyPlanId,
            SchoolVisitId       = schoolVisitId,
            ObservationRecordId = observationRecordId,
            Title               = title.Trim(),
            Description         = description?.Trim(),
            DueDate             = dueDate,
            AssignedTo          = assignedTo,
            Status              = "pending",
            CreatedBy           = createdBy
        };

        activity.AddDomainEvent(new FollowUpActivityCreatedEvent(
            activity.Id, corporationId, consultancyPlanId, schoolVisitId));

        return activity;
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Update(
        string title,
        string? description,
        DateOnly? dueDate,
        Guid? assignedTo,
        string? notes,
        Guid? updatedBy = null)
    {
        EnsureActive("update");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Follow-up activity title is required.", nameof(title));

        Title       = title.Trim();
        Description = description?.Trim();
        DueDate     = dueDate;
        AssignedTo  = assignedTo;
        Notes       = notes?.Trim();
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;
    }

    // ── Workflow ───────────────────────────────────────────────────────────────

    /// <summary>Starts work on the activity (pending → in_progress).</summary>
    public void StartProgress(Guid? updatedBy = null)
    {
        if (Status != "pending")
            throw new InvalidOperationException(
                $"Only pending activities can be started. Current status: '{Status}'.");

        Status    = "in_progress";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>Marks the activity as completed (pending|in_progress → completed).</summary>
    public void Complete(string? notes = null, Guid? completedBy = null)
    {
        EnsureActive("complete");

        Status      = "completed";
        CompletedAt = DateTimeOffset.UtcNow;
        CompletedBy = completedBy;
        if (!string.IsNullOrWhiteSpace(notes))
            Notes = notes.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = completedBy;

        AddDomainEvent(new FollowUpActivityCompletedEvent(
            Id, CorporationId, ConsultancyPlanId, SchoolVisitId));
    }

    /// <summary>Cancels the activity (pending|in_progress → cancelled).</summary>
    public void Cancel(Guid? updatedBy = null)
    {
        EnsureActive("cancel");

        Status    = "cancelled";
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    private void EnsureActive(string action)
    {
        if (Status is "completed" or "cancelled")
            throw new InvalidOperationException(
                $"Cannot {action} a '{Status}' activity.");
    }
}
