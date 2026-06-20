namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// An individual observation or question within an assessment section.
/// Maps to assessment.assessment_item.
/// ResponseType determines how the evaluator captures the answer.
/// Choices (JSONB) holds option labels for 'choice' and 'scale' response types.
/// Weight contributes to the total_score calculation for 'sum' and 'average' scoring models.
/// No audit columns in the DB — items live and die with their parent template.
/// </summary>
public class AssessmentItem : BaseEntity
{
    public Guid SectionId { get; private set; }

    /// <summary>Unique code within the section. Used as the i18n message key for the prompt.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Display text shown to the evaluator during the assessment session.</summary>
    public string Prompt { get; private set; } = string.Empty;

    /// <summary>Response capture mode. Valid values in <see cref="ResponseTypes"/>.</summary>
    public string ResponseType { get; private set; } = string.Empty;

    /// <summary>
    /// JSONB: option labels for 'choice' and 'scale' response types.
    /// Stored as a JSON string; the application layer serialises/deserialises as needed.
    /// </summary>
    public string? Choices { get; private set; }

    public decimal Weight { get; private set; } = 1m;
    public int SortOrder { get; private set; }

    public AssessmentSection Section { get; private set; } = null!;

    // ── Response type constants ───────────────────────────────────────────────
    // DB enforces CHECK(response_type in ('numeric','scale','boolean','text','choice')).
    public static class ResponseTypes
    {
        public const string Numeric = "numeric";
        public const string Scale   = "scale";
        public const string Boolean = "boolean";
        public const string Text    = "text";
        public const string Choice  = "choice";
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentItem Create(
        Guid sectionId,
        string code,
        string prompt,
        string responseType,
        string? choices = null,
        decimal weight = 1m,
        int sortOrder = 0)
    {
        ValidateResponseType(responseType);

        return new AssessmentItem
        {
            SectionId    = sectionId,
            Code         = code.Trim(),
            Prompt       = prompt.Trim(),
            ResponseType = responseType,
            Choices      = choices,
            Weight       = weight,
            SortOrder    = sortOrder
        };
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(
        string code,
        string prompt,
        string responseType,
        string? choices,
        decimal weight,
        int sortOrder)
    {
        ValidateResponseType(responseType);

        Code         = code.Trim();
        Prompt       = prompt.Trim();
        ResponseType = responseType;
        Choices      = choices;
        Weight       = weight;
        SortOrder    = sortOrder;
    }

    // ── Guard ─────────────────────────────────────────────────────────────────

    private static void ValidateResponseType(string responseType)
    {
        if (responseType is ResponseTypes.Numeric
                         or ResponseTypes.Scale
                         or ResponseTypes.Boolean
                         or ResponseTypes.Text
                         or ResponseTypes.Choice)
            return;
        throw new ArgumentException(
            $"Invalid response type '{responseType}'. Allowed: numeric, scale, boolean, text, choice.",
            nameof(responseType));
    }
}
