namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// A single evaluator response to one item within an assessment session.
/// Maps to assessment.assessment_response.
///
/// Exactly one of NumericValue, TextValue, or ChoiceValue is expected to be populated,
/// determined by the parent item's ResponseType. Multiple can be provided but
/// the scoring engine uses only the value that matches the item type.
///
/// No audit columns in the DB — responses are append-only within a session.
/// </summary>
public class AssessmentResponse : BaseEntity
{
    public Guid AssessmentSessionId { get; private set; }
    public Guid ItemId { get; private set; }

    /// <summary>Used when item.response_type is 'numeric' or 'scale'.</summary>
    public decimal? NumericValue { get; private set; }

    /// <summary>Used when item.response_type is 'text'.</summary>
    public string? TextValue { get; private set; }

    /// <summary>Used when item.response_type is 'choice' or 'boolean'.</summary>
    public string? ChoiceValue { get; private set; }

    /// <summary>Evaluator's free-text annotation for this item.</summary>
    public string? Note { get; private set; }

    public AssessmentSession Session { get; private set; } = null!;
    public AssessmentItem Item { get; private set; } = null!;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentResponse Create(
        Guid assessmentSessionId,
        Guid itemId,
        decimal? numericValue = null,
        string? textValue = null,
        string? choiceValue = null,
        string? note = null)
        => new()
        {
            AssessmentSessionId = assessmentSessionId,
            ItemId              = itemId,
            NumericValue        = numericValue,
            TextValue           = textValue,
            ChoiceValue         = choiceValue,
            Note                = note
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(
        decimal? numericValue,
        string? textValue,
        string? choiceValue,
        string? note)
    {
        NumericValue = numericValue;
        TextValue    = textValue;
        ChoiceValue  = choiceValue;
        Note         = note;
    }
}
