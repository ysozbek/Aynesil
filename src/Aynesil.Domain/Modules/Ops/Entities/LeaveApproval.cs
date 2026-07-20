namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.leave_approval.
/// Records a single approval-step decision for a leave request.
/// decision: 'approved' | 'rejected' | 'pending'.
/// step_no supports multi-step workflows (default 1 for single-level).
///
/// DDL note: table has no audit columns beyond corporation_id.
/// All AuditableEntity fields are ignored in EF configuration.
/// </summary>
public class LeaveApproval : TenantEntity
{
    public Guid LeaveRequestId { get; private set; }
    public int StepNo { get; private set; } = 1;
    public Guid? ApproverId { get; private set; }

    /// <summary>'approved' | 'rejected' | 'pending'</summary>
    public string Decision { get; private set; } = "pending";

    public string? Comment { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    public LeaveRequest? LeaveRequest { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static LeaveApproval Record(
        Guid corporationId,
        Guid leaveRequestId,
        Guid? approverId,
        string decision,
        string? comment = null,
        int stepNo = 1)
    {
        if (!ValidDecisions.Contains(decision))
            throw new ArgumentException(
                $"Invalid decision '{decision}'. Must be: approved, rejected, pending.");

        return new LeaveApproval
        {
            CorporationId  = corporationId,
            LeaveRequestId = leaveRequestId,
            StepNo         = stepNo,
            ApproverId     = approverId,
            Decision       = decision,
            Comment        = comment,
            DecidedAt      = decision != "pending" ? DateTimeOffset.UtcNow : null
        };
    }

    private static readonly string[] ValidDecisions = ["approved", "rejected", "pending"];
}
