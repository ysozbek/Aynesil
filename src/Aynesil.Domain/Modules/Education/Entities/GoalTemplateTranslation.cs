namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Per-locale display text for a goal template.
/// Maps to education.goal_template_translation.
/// Primary key is (goal_template_id, locale) — no surrogate id column in the DB.
/// Cascade-deleted when the parent template is deleted.
/// </summary>
public class GoalTemplateTranslation
{
    public Guid GoalTemplateId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string? DefaultCriteria { get; set; }

    public GoalTemplate Template { get; set; } = null!;
}
