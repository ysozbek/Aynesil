using Aynesil.Domain.Modules.Assessment.Events;

namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// A concrete assessment run performed against a lead or an enrolled student.
/// Maps to assessment.assessment_session.
///
/// Subject: lead_id OR student_id — DB enforces chk_subject (at least one must be non-null).
/// After a lead is converted to a student the session continues to reference both so
/// assessment history survives the lead→student transition.
///
/// Status transitions (DB CHECK constraint):
///   planned → in_progress → completed
///                         ↘ cancelled
///   planned              ↘ cancelled
///
/// DB columns: created_by (present), updated_by (absent), deleted_at (present, soft delete).
/// UpdatedBy is ignored in EF configuration — the DB column does not exist.
/// </summary>
public class AssessmentSession : TenantEntity
{
    public Guid TemplateId { get; private set; }

    /// <summary>
    /// Snapshot of the template version at the time the session was created.
    /// Ensures completed sessions are never affected by future template revisions.
    /// </summary>
    public int TemplateVersion { get; private set; } = 1;

    /// <summary>FK to crm.lead. NULL when the subject is an enrolled student.</summary>
    public Guid? LeadId { get; private set; }

    /// <summary>FK to students.student. NULL when the subject is a pre-enrollment lead.</summary>
    public Guid? StudentId { get; private set; }

    public Guid? CampusId { get; private set; }

    /// <summary>FK to educators.educator.id — the professional conducting the assessment.</summary>
    public Guid? AssessorId { get; private set; }

    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? PerformedAt { get; private set; }

    /// <summary>Current lifecycle status. Valid values in <see cref="SessionStatuses"/>.</summary>
    public string Status { get; private set; } = SessionStatuses.Planned;

    public decimal? TotalScore { get; private set; }

    public ICollection<AssessmentResponse> Responses { get; private set; } = [];
    public ICollection<ProgramRecommendation> Recommendations { get; private set; } = [];

    // ── Status constants ──────────────────────────────────────────────────────
    // DB enforces CHECK(status in ('planned','in_progress','completed','cancelled')).
    public static class SessionStatuses
    {
        public const string Planned    = "planned";
        public const string InProgress = "in_progress";
        public const string Completed  = "completed";
        public const string Cancelled  = "cancelled";
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentSession Create(
        Guid corporationId,
        Guid templateId,
        int templateVersion,
        Guid? leadId,
        Guid? studentId,
        Guid? campusId = null,
        Guid? assessorId = null,
        DateTimeOffset? scheduledAt = null,
        Guid? createdBy = null)
    {
        if (leadId is null && studentId is null)
            throw new ArgumentException(
                "An assessment session must have either a lead or a student as its subject.");

        var session = new AssessmentSession
        {
            CorporationId   = corporationId,
            TemplateId      = templateId,
            TemplateVersion = templateVersion,
            LeadId          = leadId,
            StudentId       = studentId,
            CampusId        = campusId,
            AssessorId      = assessorId,
            ScheduledAt     = scheduledAt,
            Status          = SessionStatuses.Planned,
            CreatedAt       = DateTimeOffset.UtcNow,
            UpdatedAt       = DateTimeOffset.UtcNow,
            CreatedBy       = createdBy
        };

        session.AddDomainEvent(new AssessmentSessionCreatedEvent(
            session.Id, corporationId, templateId, leadId, studentId, createdBy));

        return session;
    }

    // ── Workflow transitions ───────────────────────────────────────────────────

    public void Start(Guid? updatedBy = null)
    {
        if (Status != SessionStatuses.Planned)
            throw new InvalidOperationException(
                $"Cannot start a session in '{Status}' status. Only 'planned' sessions can be started.");

        Status    = SessionStatuses.InProgress;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new AssessmentSessionStartedEvent(Id, CorporationId, updatedBy));
    }

    public void Complete(decimal? totalScore, Guid? updatedBy = null)
    {
        if (Status != SessionStatuses.InProgress)
            throw new InvalidOperationException(
                $"Cannot complete a session in '{Status}' status. Only 'in_progress' sessions can be completed.");

        Status      = SessionStatuses.Completed;
        PerformedAt = DateTimeOffset.UtcNow;
        TotalScore  = totalScore;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;

        AddDomainEvent(new AssessmentSessionCompletedEvent(
            Id, CorporationId, LeadId, StudentId, totalScore, updatedBy));
    }

    public void Cancel(Guid? updatedBy = null)
    {
        if (Status is SessionStatuses.Completed or SessionStatuses.Cancelled)
            throw new InvalidOperationException(
                $"Cannot cancel a session in '{Status}' status.");

        Status    = SessionStatuses.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new AssessmentSessionCancelledEvent(Id, CorporationId, updatedBy));
    }

    public void UpdateSchedule(
        DateTimeOffset? scheduledAt,
        Guid? assessorId,
        Guid? campusId,
        Guid? updatedBy = null)
    {
        if (Status is SessionStatuses.Completed or SessionStatuses.Cancelled)
            throw new InvalidOperationException(
                "Cannot reschedule a completed or cancelled session.");

        ScheduledAt = scheduledAt;
        AssessorId  = assessorId;
        CampusId    = campusId;
        UpdatedAt   = DateTimeOffset.UtcNow;
        UpdatedBy   = updatedBy;
    }

    /// <summary>
    /// Links the session to the converted student record after lead→student conversion.
    /// Preserves historical assessment data under the new student identity.
    /// </summary>
    public void LinkToStudent(Guid studentId, Guid? updatedBy = null)
    {
        StudentId = studentId;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
