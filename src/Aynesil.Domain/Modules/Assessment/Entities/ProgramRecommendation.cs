using Aynesil.Domain.Modules.Assessment.Events;

namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// Program recommendation produced from an assessment — feeds the enrollment workflow.
/// Maps to assessment.program_recommendation.
///
/// Can reference a lead (pre-enrollment assessment) or a student (re-assessment after enrollment).
/// recommended_program_id is a soft FK to education.program — no EF navigation property
/// is added here to avoid a cross-module circular dependency. The education module
/// resolves program details independently.
///
/// DB columns present: id, corporation_id, assessment_session_id, lead_id, student_id,
///   recommended_program_id, recommended_intensity, rationale, recommended_by,
///   created_at, updated_at, row_version.
/// DB columns absent: created_by, updated_by, deleted_at.
/// CreatedBy / UpdatedBy from AuditableEntity are ignored in EF configuration.
/// </summary>
public class ProgramRecommendation : AuditableEntity
{
    public Guid CorporationId { get; private set; }

    /// <summary>The session that produced this recommendation. Optional if created standalone.</summary>
    public Guid? AssessmentSessionId { get; private set; }

    public Guid? LeadId { get; private set; }
    public Guid? StudentId { get; private set; }

    /// <summary>
    /// Soft FK to education.program.id. No EF navigation property — avoids cross-module coupling.
    /// The enrollment module resolves the referenced program independently.
    /// </summary>
    public Guid? RecommendedProgramId { get; private set; }

    public string? RecommendedIntensity { get; private set; }
    public string? Rationale { get; private set; }

    /// <summary>FK to educators.educator.id — the professional issuing the recommendation.</summary>
    public Guid? RecommendedBy { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ProgramRecommendation Create(
        Guid corporationId,
        Guid? assessmentSessionId = null,
        Guid? leadId = null,
        Guid? studentId = null,
        Guid? recommendedProgramId = null,
        string? recommendedIntensity = null,
        string? rationale = null,
        Guid? recommendedBy = null,
        Guid? createdBy = null)
    {
        var rec = new ProgramRecommendation
        {
            CorporationId          = corporationId,
            AssessmentSessionId    = assessmentSessionId,
            LeadId                 = leadId,
            StudentId              = studentId,
            RecommendedProgramId   = recommendedProgramId,
            RecommendedIntensity   = recommendedIntensity,
            Rationale              = rationale,
            RecommendedBy          = recommendedBy,
            CreatedAt              = DateTimeOffset.UtcNow,
            UpdatedAt              = DateTimeOffset.UtcNow,
            CreatedBy              = createdBy
        };

        rec.AddDomainEvent(new ProgramRecommendationCreatedEvent(
            rec.Id, corporationId, assessmentSessionId, leadId, studentId, recommendedProgramId));

        return rec;
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(
        Guid? recommendedProgramId,
        string? recommendedIntensity,
        string? rationale,
        Guid? recommendedBy,
        Guid? updatedBy = null)
    {
        RecommendedProgramId = recommendedProgramId;
        RecommendedIntensity = recommendedIntensity;
        Rationale            = rationale;
        RecommendedBy        = recommendedBy;
        UpdatedAt            = DateTimeOffset.UtcNow;
        UpdatedBy            = updatedBy;
    }
}
