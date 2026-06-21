using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Scheduling.Events;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// Tracks a missed session through makeup scheduling and completion.
/// missed_reason_id references ref.ref_value (ref_type 'missed_reason') — configurable.
///
/// Status lifecycle:
///   requested → approved → scheduled → completed
///   requested → rejected
///   approved  → expired  (if not scheduled before expires_on)
///
/// Maps to scheduling.makeup_request.
/// Audit: requested_at, updated_at, row_version (no created_by / deleted_at in DDL).
/// </summary>
public class MakeupRequest : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid? MissedSessionId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'missed_reason'). Configurable.</summary>
    public Guid? MissedReasonId { get; private set; }

    /// <summary>requested | approved | scheduled | completed | rejected | expired</summary>
    public string Status { get; private set; } = "requested";

    public Guid? RequestedBy { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>The makeup session scheduled to compensate the missed one.</summary>
    public Guid? MakeupSessionId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public string? Note { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; private set; } = 1;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MakeupRequest Create(
        Guid corporationId,
        Guid studentId,
        Guid? missedSessionId = null,
        Guid? missedReasonId = null,
        string? note = null,
        DateOnly? expiresOn = null,
        Guid? requestedBy = null)
    {
        var request = new MakeupRequest
        {
            CorporationId  = corporationId,
            StudentId      = studentId,
            MissedSessionId = missedSessionId,
            MissedReasonId = missedReasonId,
            Status         = "requested",
            RequestedBy    = requestedBy,
            RequestedAt    = DateTimeOffset.UtcNow,
            ExpiresOn      = expiresOn,
            Note           = note,
            UpdatedAt      = DateTimeOffset.UtcNow
        };

        request.AddDomainEvent(new MakeupRequestedEvent(
            request.Id, corporationId, studentId, missedSessionId, requestedBy));

        return request;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Approve()
    {
        EnsureStatus("requested");
        Status    = "approved";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? note = null)
    {
        if (Status is "completed" or "rejected")
            throw new InvalidOperationException($"Cannot reject a makeup request in '{Status}' status.");

        Status    = "rejected";
        Note      = note ?? Note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignMakeupSession(Guid makeupSessionId)
    {
        EnsureStatus("approved");

        MakeupSessionId = makeupSessionId;
        Status          = "scheduled";
        UpdatedAt       = DateTimeOffset.UtcNow;

        AddDomainEvent(new MakeupScheduledEvent(
            Id, CorporationId, StudentId, makeupSessionId));
    }

    public void MarkCompleted()
    {
        EnsureStatus("scheduled");

        Status      = "completed";
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        if (Status is "completed" or "rejected" or "expired")
            throw new InvalidOperationException($"Cannot expire makeup request in '{Status}' status.");

        Status    = "expired";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private void EnsureStatus(string expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Makeup request must be in '{expected}' status for this operation. Current: '{Status}'.");
    }
}
