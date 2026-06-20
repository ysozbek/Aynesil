namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// A logical grouping of items within an assessment template.
/// Maps to assessment.assessment_section.
/// Sections are ordered by SortOrder.
/// DevelopmentAreaId links to ref_type 'development_area' — configurable, not hardcoded.
/// No audit columns in the DB — sections live and die with their parent template.
/// </summary>
public class AssessmentSection : BaseEntity
{
    public Guid TemplateId { get; private set; }

    /// <summary>Unique code within the template. Used as the i18n message key for display.</summary>
    public string Code { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    /// <summary>ref_type 'development_area' — optional developmental domain for this section.</summary>
    public Guid? DevelopmentAreaId { get; private set; }

    public AssessmentTemplate Template { get; private set; } = null!;
    public ICollection<AssessmentItem> Items { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentSection Create(
        Guid templateId,
        string code,
        int sortOrder = 0,
        Guid? developmentAreaId = null)
        => new()
        {
            TemplateId        = templateId,
            Code              = code.Trim(),
            SortOrder         = sortOrder,
            DevelopmentAreaId = developmentAreaId
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(string code, int sortOrder, Guid? developmentAreaId)
    {
        Code              = code.Trim();
        SortOrder         = sortOrder;
        DevelopmentAreaId = developmentAreaId;
    }
}
