using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Crm.Entities;

/// <summary>
/// Immutable audit trail for every status / pipeline-stage transition on a lead.
/// Maps to crm.lead_status_history. Append-only — no soft delete, no row_version.
/// </summary>
public class LeadStatusHistory : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid LeadId { get; private set; }

    /// <summary>The status the lead moved to — ref_type 'lead_status'.</summary>
    public Guid? StatusId { get; private set; }

    /// <summary>The pipeline stage at the time of the change — ref_type 'pipeline_stage'.</summary>
    public Guid? PipelineStageId { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? ChangedBy { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public Lead? Lead { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static LeadStatusHistory Record(
        Guid corporationId,
        Guid leadId,
        Guid? statusId,
        Guid? pipelineStageId,
        Guid? changedBy = null)
        => new()
        {
            CorporationId = corporationId,
            LeadId = leadId,
            StatusId = statusId,
            PipelineStageId = pipelineStageId,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = changedBy
        };
}
