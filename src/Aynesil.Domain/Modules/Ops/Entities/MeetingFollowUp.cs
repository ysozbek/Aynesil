using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting_follow_up.
/// An action item created after a meeting, assigned to a staff member with a due date.
/// Status workflow: open → in_progress → done | cancelled.
/// DDL: minimal audit — only created_at; no created_by, updated_at, deleted_at, or row_version.
/// Inherits BaseEntity for Id. Does NOT inherit TenantEntity/AuditableEntity because the DDL
/// lacks the full audit-column set; fields are declared directly.
/// </summary>
public class MeetingFollowUp : BaseEntity
{
    private static readonly string[] ValidStatuses =
        ["open", "in_progress", "done", "cancelled"];

    public Guid CorporationId { get; private set; }
    public Guid MeetingId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid? AssigneeId { get; private set; }

    /// <summary>Due date (date only, no time component).</summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>'open' | 'in_progress' | 'done' | 'cancelled'.</summary>
    public string Status { get; private set; } = "open";

    public DateTimeOffset CreatedAt { get; private set; }

    public Meeting? Meeting { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MeetingFollowUp Create(
        Guid corporationId,
        Guid meetingId,
        string action,
        Guid? assigneeId = null,
        DateOnly? dueDate = null)
        => new()
        {
            CorporationId = corporationId,
            MeetingId     = meetingId,
            Action        = action.Trim(),
            AssigneeId    = assigneeId,
            DueDate       = dueDate,
            Status        = "open",
            CreatedAt     = DateTimeOffset.UtcNow
        };

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(string action, Guid? assigneeId, DateOnly? dueDate)
    {
        if (Status is "done" or "cancelled")
            throw new InvalidOperationException("Cannot update a closed follow-up.");

        Action     = action.Trim();
        AssigneeId = assigneeId;
        DueDate    = dueDate;
    }

    public void UpdateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid status '{status}'. Must be: open, in_progress, done, cancelled.");

        if (Status == "done" && status != "done")
            throw new InvalidOperationException("Cannot reopen a completed follow-up.");

        if (Status == "cancelled" && status != "cancelled")
            throw new InvalidOperationException("Cannot reopen a cancelled follow-up.");

        Status = status;
    }
}
