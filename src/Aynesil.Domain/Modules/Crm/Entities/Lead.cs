using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Crm.Events;

namespace Aynesil.Domain.Modules.Crm.Entities;

/// <summary>
/// Prospect / lead record. Maps to crm.lead.
/// The lead belongs to a corporation (tenant) and optionally a campus (branch).
/// All ref-data lookups (source, status, pipeline stage) use ref.ref_value FKs
/// so they remain configurable without schema changes.
/// On enrollment the lead is linked to students.student via ConvertedStudentId.
/// </summary>
public class Lead : TenantBranchEntity
{
    /// <summary>Where the lead originated — ref_type 'lead_source'.</summary>
    public Guid? SourceId { get; private set; }

    /// <summary>Current qualification status — ref_type 'lead_status'.</summary>
    public Guid? StatusId { get; private set; }

    /// <summary>Current stage in the admissions funnel — ref_type 'pipeline_stage'.</summary>
    public Guid? PipelineStageId { get; private set; }

    public string? ChildName { get; private set; }
    public DateOnly? ChildBirthDate { get; private set; }

    public string ContactName { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }

    /// <summary>Free-text description of the presenting need / reason for inquiry.</summary>
    public string? PresentingNeed { get; private set; }

    /// <summary>Detail about the referral source when source is 'referral'.</summary>
    public string? ReferralDetail { get; private set; }

    /// <summary>FK to iam.user_account — the staff member responsible for this lead.</summary>
    public Guid? AssignedToId { get; private set; }

    /// <summary>Optional qualification score (0–100).</summary>
    public int? Score { get; private set; }

    /// <summary>Set when the lead is converted. FK to students.student (Layer 2).</summary>
    public Guid? ConvertedStudentId { get; private set; }
    public DateTimeOffset? ConvertedAt { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ICollection<LeadStatusHistory> StatusHistory { get; private set; } = [];
    public ICollection<LeadActivity> Activities { get; private set; } = [];
    public ICollection<Interview> Interviews { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Lead Create(
        Guid corporationId,
        string contactName,
        Guid? campusId = null,
        Guid? sourceId = null,
        Guid? statusId = null,
        Guid? pipelineStageId = null,
        string? childName = null,
        DateOnly? childBirthDate = null,
        string? contactPhone = null,
        string? contactEmail = null,
        string? presentingNeed = null,
        string? referralDetail = null,
        Guid? assignedToId = null,
        int? score = null,
        Guid? createdBy = null)
    {
        var lead = new Lead
        {
            CorporationId = corporationId,
            CampusId = campusId,
            SourceId = sourceId,
            StatusId = statusId,
            PipelineStageId = pipelineStageId,
            ChildName = childName,
            ChildBirthDate = childBirthDate,
            ContactName = contactName,
            ContactPhone = contactPhone,
            ContactEmail = contactEmail,
            PresentingNeed = presentingNeed,
            ReferralDetail = referralDetail,
            AssignedToId = assignedToId,
            Score = score,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        lead.AddDomainEvent(new LeadCreatedEvent(lead.Id, corporationId, contactName, sourceId));
        return lead;
    }

    // ── Mutation methods ──────────────────────────────────────────────────────

    public void Update(
        string contactName,
        Guid? campusId,
        Guid? sourceId,
        string? childName,
        DateOnly? childBirthDate,
        string? contactPhone,
        string? contactEmail,
        string? presentingNeed,
        string? referralDetail,
        Guid? assignedToId,
        int? score,
        Guid? updatedBy = null)
    {
        ContactName = contactName;
        CampusId = campusId;
        SourceId = sourceId;
        ChildName = childName;
        ChildBirthDate = childBirthDate;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        PresentingNeed = presentingNeed;
        ReferralDetail = referralDetail;
        AssignedToId = assignedToId;
        Score = score;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Moves the lead to a new status and optionally advances the pipeline stage.
    /// Records a LeadStatusHistory entry via the domain event.
    /// </summary>
    public void ChangeStatus(
        Guid newStatusId,
        Guid? newPipelineStageId = null,
        Guid? changedBy = null)
    {
        var previousStatusId = StatusId;
        var previousPipelineStageId = PipelineStageId;

        StatusId = newStatusId;
        if (newPipelineStageId.HasValue)
            PipelineStageId = newPipelineStageId.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = changedBy;

        AddDomainEvent(new LeadStatusChangedEvent(
            Id, CorporationId,
            previousStatusId, newStatusId,
            previousPipelineStageId, newPipelineStageId,
            changedBy));
    }

    /// <summary>
    /// Assigns the lead to a staff member.
    /// </summary>
    public void Assign(Guid userId, Guid? updatedBy = null)
    {
        AssignedToId = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Links the lead to a newly created student record, marking the conversion.
    /// Can only be called once.
    /// </summary>
    public void ConvertToStudent(Guid studentId, Guid? convertedBy = null)
    {
        if (ConvertedStudentId.HasValue)
            throw new InvalidOperationException("Lead is already converted to a student.");

        ConvertedStudentId = studentId;
        ConvertedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = convertedBy;

        AddDomainEvent(new LeadConvertedEvent(Id, CorporationId, studentId, convertedBy));
    }
}
