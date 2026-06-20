using Aynesil.Domain.Modules.Assessment.Events;

namespace Aynesil.Domain.Modules.Assessment.Entities;

/// <summary>
/// Reusable assessment blueprint. Maps to assessment.assessment_template.
/// corporation_id = NULL means a platform-provided template available to all tenants.
/// Tenant-specific templates are scoped to one corporation.
/// type_id → ref_type 'assessment_type', category_id → ref_type 'assessment_category'.
/// Templates are versioned: creating a new version deactivates the current one and
/// produces a new row with an incremented version number (same code, version+1).
/// No deleted_at column — deactivation (IsActive = false) is the lifecycle mechanism.
/// </summary>
public class AssessmentTemplate : AuditableEntity
{
    /// <summary>
    /// Tenant scope. NULL = platform-provided template visible to all tenants.
    /// Set = scoped to one corporation.
    /// </summary>
    public Guid? CorporationId { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>ref_type 'assessment_type' — configurable, never hardcoded.</summary>
    public Guid? TypeId { get; private set; }

    /// <summary>ref_type 'assessment_category' — configurable, never hardcoded.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>Scoring algorithm. Valid values defined in <see cref="ScoringModels"/>.</summary>
    public string? ScoringModel { get; private set; }

    public int Version { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    public ICollection<AssessmentTemplateTranslation> Translations { get; private set; } = [];
    public ICollection<AssessmentSection> Sections { get; private set; } = [];

    // ── Scoring model constants ───────────────────────────────────────────────
    // DB enforces CHECK(scoring_model in ('sum','average','rubric','none')).
    // String constants rather than enums per project rules.
    public static class ScoringModels
    {
        public const string Sum     = "sum";
        public const string Average = "average";
        public const string Rubric  = "rubric";
        public const string None    = "none";
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AssessmentTemplate Create(
        string code,
        string name,
        Guid? corporationId = null,
        Guid? typeId = null,
        Guid? categoryId = null,
        string? scoringModel = null,
        Guid? createdBy = null)
    {
        ValidateScoringModel(scoringModel);

        var template = new AssessmentTemplate
        {
            CorporationId = corporationId,
            Code          = code.Trim(),
            Name          = name.Trim(),
            TypeId        = typeId,
            CategoryId    = categoryId,
            ScoringModel  = scoringModel,
            Version       = 1,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = createdBy
        };

        return template;
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? typeId,
        Guid? categoryId,
        string? scoringModel,
        Guid? updatedBy = null)
    {
        ValidateScoringModel(scoringModel);

        Name         = name.Trim();
        TypeId       = typeId;
        CategoryId   = categoryId;
        ScoringModel = scoringModel;
        UpdatedAt    = DateTimeOffset.UtcNow;
        UpdatedBy    = updatedBy;
    }

    public void Activate(Guid? updatedBy = null)
    {
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(Guid? updatedBy = null)
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpsertTranslation(string locale, string name, string? description)
    {
        var existing = Translations.FirstOrDefault(t => t.Locale == locale);
        if (existing is not null)
        {
            existing.Name        = name;
            existing.Description = description;
        }
        else
        {
            Translations.Add(new AssessmentTemplateTranslation
            {
                TemplateId  = Id,
                Locale      = locale,
                Name        = name,
                Description = description
            });
        }
    }

    /// <summary>
    /// Spawns a new template row with the same code and version+1.
    /// Deactivates this template so only the newest version is active.
    /// Used for the reassessment workflow where an updated template must not
    /// invalidate existing completed sessions that reference the old version.
    /// </summary>
    public AssessmentTemplate CreateNewVersion(Guid? createdBy = null)
    {
        Deactivate(createdBy);

        return new AssessmentTemplate
        {
            CorporationId = CorporationId,
            Code          = Code,
            Name          = Name,
            TypeId        = TypeId,
            CategoryId    = CategoryId,
            ScoringModel  = ScoringModel,
            Version       = Version + 1,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = createdBy
        };
    }

    // ── Guard ─────────────────────────────────────────────────────────────────

    private static void ValidateScoringModel(string? model)
    {
        if (model is null) return;
        if (model is ScoringModels.Sum or ScoringModels.Average or ScoringModels.Rubric or ScoringModels.None)
            return;
        throw new ArgumentException(
            $"Invalid scoring model '{model}'. Allowed: sum, average, rubric, none.", nameof(model));
    }
}
