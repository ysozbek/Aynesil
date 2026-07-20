namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.survey_answer_option.
/// Pre-defined answer options for 'multiple_choice' and 'scale' question types.
/// No corporation_id — isolation inherited from grandparent SurveyDefinition via cascade.
/// OptionValue is an optional scoring/code value (e.g. "5" for a scale option labelled "Excellent").
/// </summary>
public class SurveyAnswerOption : BaseEntity
{
    public Guid QuestionId { get; private set; }

    public string OptionText { get; private set; } = string.Empty;

    /// <summary>Optional numeric or code value used for scoring/reporting.</summary>
    public string? OptionValue { get; private set; }

    public int SortOrder { get; private set; }

    public SurveyQuestion? Question { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SurveyAnswerOption Create(
        Guid questionId,
        string optionText,
        string? optionValue = null,
        int sortOrder = 0)
        => new()
        {
            QuestionId  = questionId,
            OptionText  = optionText.Trim(),
            OptionValue = optionValue,
            SortOrder   = sortOrder
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(string optionText, string? optionValue, int sortOrder)
    {
        OptionText  = optionText.Trim();
        OptionValue = optionValue;
        SortOrder   = sortOrder;
    }
}
