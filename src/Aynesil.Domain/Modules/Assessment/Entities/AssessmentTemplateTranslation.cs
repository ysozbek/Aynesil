namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// Per-locale display text for an assessment template.
/// Maps to assessment.assessment_template_translation.
/// Primary key is (template_id, locale) — there is no surrogate id column in the DB.
/// Cascade-deleted when the parent template is deleted.
/// </summary>
public class AssessmentTemplateTranslation
{
    public Guid TemplateId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public AssessmentTemplate Template { get; set; } = null!;
}
