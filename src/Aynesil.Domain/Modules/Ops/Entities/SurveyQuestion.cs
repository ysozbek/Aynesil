namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.survey_question.
/// A single question within a SurveyDefinition. No corporation_id — tenant
/// isolation is inherited from the parent SurveyDefinition via cascade + parent RLS.
/// Emsal: assessment.assessment_section.
/// question_type: 'text' | 'rating' | 'yes_no' | 'multiple_choice' | 'scale'.
/// </summary>
public class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; private set; }

    public string QuestionText { get; private set; } = string.Empty;

    /// <summary>'text' | 'rating' | 'yes_no' | 'multiple_choice' | 'scale'.</summary>
    public string QuestionType { get; private set; } = "text";

    public bool IsRequired { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }

    public SurveyDefinition? Survey { get; private set; }
    public ICollection<SurveyAnswerOption> AnswerOptions { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SurveyQuestion Create(
        Guid surveyId,
        string questionText,
        string questionType = "text",
        bool isRequired = false,
        int sortOrder = 0,
        Guid? createdBy = null)
        => new()
        {
            SurveyId     = surveyId,
            QuestionText = questionText.Trim(),
            QuestionType = questionType,
            IsRequired   = isRequired,
            SortOrder    = sortOrder,
            CreatedAt    = DateTimeOffset.UtcNow,
            CreatedBy    = createdBy
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(string questionText, string questionType, bool isRequired, int sortOrder)
    {
        QuestionText = questionText.Trim();
        QuestionType = questionType;
        IsRequired   = isRequired;
        SortOrder    = sortOrder;
    }
}
