using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.meeting_outcome.
/// The recorded outcome of a meeting: free-text summary and decisions.
/// DDL: minimal audit — only created_at and created_by; no updated_at, deleted_at, or row_version.
/// Inherits BaseEntity for Id. Does NOT inherit TenantEntity/AuditableEntity because the DDL
/// lacks the full audit-column set; fields are declared directly.
/// </summary>
public class MeetingOutcome : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid MeetingId { get; private set; }

    public string? Summary { get; private set; }
    public string? Decisions { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public Meeting? Meeting { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static MeetingOutcome Create(
        Guid corporationId,
        Guid meetingId,
        string? summary,
        string? decisions,
        Guid? createdBy = null)
        => new()
        {
            CorporationId = corporationId,
            MeetingId     = meetingId,
            Summary       = summary?.Trim(),
            Decisions     = decisions?.Trim(),
            CreatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = createdBy
        };

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(string? summary, string? decisions)
    {
        Summary   = summary?.Trim();
        Decisions = decisions?.Trim();
    }
}
