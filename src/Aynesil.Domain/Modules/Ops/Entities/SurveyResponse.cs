namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.survey_response.
/// One completed (or in-progress) form submission per survey × respondent × optional session.
/// SubmittedAt is null while the respondent is mid-form; set when the form is submitted.
/// Inherits TenantEntity for corporation_id + RLS; unused audit columns are ignored in EF config.
/// </summary>
public class SurveyResponse : TenantEntity
{
    public Guid SurveyId { get; private set; }

    /// <summary>IAM user account of the person who submitted the form.</summary>
    public Guid? RespondentUserId { get; private set; }

    /// <summary>Set when the respondent is a guardian (parent portal submission).</summary>
    public Guid? GuardianId { get; private set; }

    /// <summary>The student this feedback relates to (if applicable).</summary>
    public Guid? StudentId { get; private set; }

    /// <summary>The session this feedback relates to (if applicable).</summary>
    public Guid? SessionId { get; private set; }

    /// <summary>Null while in-progress; set when the respondent finalises the form.</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SurveyDefinition? Survey { get; private set; }
    public ICollection<SurveyQuestionResponse> QuestionResponses { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SurveyResponse Create(
        Guid corporationId,
        Guid surveyId,
        Guid? respondentUserId = null,
        Guid? guardianId = null,
        Guid? studentId = null,
        Guid? sessionId = null)
        => new()
        {
            CorporationId     = corporationId,
            SurveyId          = surveyId,
            RespondentUserId  = respondentUserId,
            GuardianId        = guardianId,
            StudentId         = studentId,
            SessionId         = sessionId,
            SubmittedAt       = null,
            CreatedAt         = DateTimeOffset.UtcNow
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Submit()
    {
        if (SubmittedAt.HasValue)
            throw new InvalidOperationException("Survey response has already been submitted.");

        SubmittedAt = DateTimeOffset.UtcNow;
    }
}
