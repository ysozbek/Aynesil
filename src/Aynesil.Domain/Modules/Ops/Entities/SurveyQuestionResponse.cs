namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.survey_question_response.
/// Individual answer to one question within a SurveyResponse.
/// Exactly one of AnswerText / AnswerOptionId / NumericValue should be populated,
/// determined by the parent question's QuestionType:
///   text, yes_no   → AnswerText
///   multiple_choice → AnswerOptionId
///   rating, scale   → NumericValue
/// No corporation_id — isolation inherited from SurveyResponse via cascade.
/// </summary>
public class SurveyQuestionResponse : BaseEntity
{
    public Guid ResponseId { get; private set; }
    public Guid QuestionId { get; private set; }

    /// <summary>Used for 'text' and 'yes_no' question types.</summary>
    public string? AnswerText { get; private set; }

    /// <summary>Used for 'multiple_choice' question type; FK to SurveyAnswerOption.</summary>
    public Guid? AnswerOptionId { get; private set; }

    /// <summary>Used for 'rating' and 'scale' question types.</summary>
    public decimal? NumericValue { get; private set; }

    public SurveyResponse? Response { get; private set; }
    public SurveyQuestion? Question { get; private set; }
    public SurveyAnswerOption? AnswerOption { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SurveyQuestionResponse Create(
        Guid responseId,
        Guid questionId,
        string? answerText = null,
        Guid? answerOptionId = null,
        decimal? numericValue = null)
        => new()
        {
            ResponseId     = responseId,
            QuestionId     = questionId,
            AnswerText     = answerText,
            AnswerOptionId = answerOptionId,
            NumericValue   = numericValue
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(string? answerText, Guid? answerOptionId, decimal? numericValue)
    {
        AnswerText     = answerText;
        AnswerOptionId = answerOptionId;
        NumericValue   = numericValue;
    }
}
