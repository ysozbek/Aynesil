using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Individualized Education Plan (BEP/IEP) for a student.
/// Versioned — each revision increments Version and creates an EducationPlanRevision snapshot.
///
/// Status workflow:
///   draft → in_review → approved → active → revised | closed
/// Invalid transitions are enforced in domain methods.
///
/// Guardian visibility: guardian_visible is set only when status = approved, at coordinator discretion.
/// Maps to education.education_plan.
///
/// Audit: full (created_at, created_by, updated_at, updated_by, deleted_at, row_version).
/// </summary>
public class EducationPlan : TenantEntity
{
    public Guid StudentId { get; private set; }

    /// <summary>FK to education.academic_period. Scopes the plan to a term.</summary>
    public Guid? AcademicPeriodId { get; private set; }

    /// <summary>FK to core.campus. The campus where the plan is being delivered.</summary>
    public Guid? CampusId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Incremented on each revision. Starts at 1.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>draft | in_review | approved | active | revised | closed</summary>
    public string Status { get; private set; } = "draft";

    public DateOnly? EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    /// <summary>FK to educators.educator — the specialist who prepared the plan.</summary>
    public Guid? PreparedBy { get; private set; }

    /// <summary>FK to educators.educator — the coordinator who approved the plan.</summary>
    public Guid? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>
    /// When true and status = approved, guardians with portal access may view the plan.
    /// Set explicitly by coordinator via MakeGuardianVisible().
    /// </summary>
    public bool GuardianVisible { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<EducationPlanGoal> PlanGoals { get; private set; } = [];
    public ICollection<EducationPlanReview> Reviews { get; private set; } = [];
    public ICollection<EducationPlanApproval> Approvals { get; private set; } = [];
    public ICollection<EducationPlanRevision> Revisions { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducationPlan Create(
        Guid corporationId,
        Guid studentId,
        string title,
        Guid? academicPeriodId = null,
        Guid? campusId = null,
        Guid? preparedBy = null,
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveTo = null,
        Guid? createdBy = null)
    {
        var plan = new EducationPlan
        {
            CorporationId    = corporationId,
            StudentId        = studentId,
            AcademicPeriodId = academicPeriodId,
            CampusId         = campusId,
            Title            = title,
            Version          = 1,
            Status           = "draft",
            PreparedBy       = preparedBy,
            EffectiveFrom    = effectiveFrom,
            EffectiveTo      = effectiveTo,
            GuardianVisible  = false,
            CreatedAt        = DateTimeOffset.UtcNow,
            UpdatedAt        = DateTimeOffset.UtcNow,
            CreatedBy        = createdBy
        };

        plan.AddDomainEvent(new EducationPlanCreatedEvent(
            plan.Id, corporationId, studentId, title, createdBy));

        return plan;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateDetails(
        string title,
        Guid? academicPeriodId,
        Guid? campusId,
        Guid? preparedBy,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo,
        Guid? updatedBy = null)
    {
        GuardDraftOrInReview("update details");

        Title            = title;
        AcademicPeriodId = academicPeriodId;
        CampusId         = campusId;
        PreparedBy       = preparedBy;
        EffectiveFrom    = effectiveFrom;
        EffectiveTo      = effectiveTo;
        UpdatedAt        = DateTimeOffset.UtcNow;
        UpdatedBy        = updatedBy;
    }

    public void SubmitForReview(Guid? updatedBy = null)
    {
        if (Status != "draft")
            throw new InvalidOperationException(
                $"Only a draft plan can be submitted for review. Current status: {Status}.");

        ChangeStatus("in_review", updatedBy);
    }

    public void Approve(Guid approverId, Guid? updatedBy = null)
    {
        if (Status != "in_review")
            throw new InvalidOperationException(
                $"Only a plan in_review can be approved. Current status: {Status}.");

        ApprovedBy  = approverId;
        ApprovedAt  = DateTimeOffset.UtcNow;
        ChangeStatus("approved", updatedBy);

        AddDomainEvent(new EducationPlanApprovedEvent(
            Id, CorporationId, StudentId, approverId, Version, updatedBy));
    }

    public void Reject(Guid? updatedBy = null)
    {
        if (Status != "in_review")
            throw new InvalidOperationException(
                $"Only a plan in_review can be rejected. Current status: {Status}.");

        ChangeStatus("draft", updatedBy);
    }

    public void Activate(Guid? updatedBy = null)
    {
        if (Status != "approved")
            throw new InvalidOperationException(
                $"Only an approved plan can be activated. Current status: {Status}.");

        ChangeStatus("active", updatedBy);
    }

    public void Close(Guid? updatedBy = null)
    {
        if (Status is "draft" or "revised" or "closed")
            throw new InvalidOperationException(
                $"Plan in status '{Status}' cannot be closed.");

        ChangeStatus("closed", updatedBy);
    }

    /// <summary>
    /// Bumps the version, sets status to 'revised', and returns the revision header.
    /// The caller is responsible for persisting the EducationPlanRevision snapshot.
    /// </summary>
    public EducationPlanRevision CreateRevision(string? changeSummary, object? snapshot, Guid educatorId)
    {
        if (Status is "draft" or "closed")
            throw new InvalidOperationException(
                $"A plan in status '{Status}' cannot be revised.");

        var from = Version;
        Version++;

        var revision = new EducationPlanRevision(
            CorporationId, Id, from, Version, changeSummary, snapshot, educatorId);

        Revisions.Add(revision);
        ChangeStatus("revised", educatorId);
        return revision;
    }

    public void MakeGuardianVisible(Guid? updatedBy = null)
    {
        if (Status is not ("approved" or "active"))
            throw new InvalidOperationException(
                "Guardian visibility can only be enabled for approved or active plans.");

        GuardianVisible = true;
        UpdatedAt       = DateTimeOffset.UtcNow;
        UpdatedBy       = updatedBy;
    }

    public void RevokeGuardianVisibility(Guid? updatedBy = null)
    {
        GuardianVisible = false;
        UpdatedAt       = DateTimeOffset.UtcNow;
        UpdatedBy       = updatedBy;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ChangeStatus(string newStatus, Guid? updatedBy)
    {
        var previous = Status;
        Status    = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new EducationPlanStatusChangedEvent(
            Id, CorporationId, StudentId, previous, newStatus, updatedBy));
    }

    private void GuardDraftOrInReview(string operation)
    {
        if (Status is not ("draft" or "in_review"))
            throw new InvalidOperationException(
                $"Cannot {operation} on a plan with status '{Status}'.");
    }
}
