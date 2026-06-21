namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// A reusable goal template that can be instantiated as a StudentGoal.
/// CorporationId = NULL = platform-provided template.
/// Category (goal_category) and DevelopmentArea (development_area) are configurable ref data.
/// Translations for localized statement / criteria stored in GoalTemplateTranslation.
/// Maps to education.goal_template.
///
/// Audit: created_at, updated_at, deleted_at, row_version.
/// Absent from DDL (ignored in config): created_by, updated_by.
/// Soft delete: deleted_at.
/// </summary>
public class GoalTemplate : SoftDeleteEntity
{
    /// <summary>NULL = platform-provided template.</summary>
    public Guid? CorporationId { get; private set; }

    /// <summary>FK to education.goal_library. Optional — a template may exist outside a library.</summary>
    public Guid? LibraryId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'goal_category'). Configurable.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'development_area'). Configurable.</summary>
    public Guid? DevelopmentAreaId { get; private set; }

    /// <summary>Human-readable code, e.g. "LANG-01". Optional.</summary>
    public string? Code { get; private set; }

    /// <summary>Default English statement. Locale-specific text in GoalTemplateTranslation.</summary>
    public string Statement { get; private set; } = string.Empty;

    /// <summary>Default mastery criteria text.</summary>
    public string? DefaultCriteria { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public GoalLibrary? Library { get; private set; }
    public ICollection<GoalTemplateTranslation> Translations { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static GoalTemplate Create(
        Guid? corporationId,
        string statement,
        Guid? libraryId = null,
        Guid? categoryId = null,
        Guid? developmentAreaId = null,
        string? code = null,
        string? defaultCriteria = null)
    {
        return new GoalTemplate
        {
            CorporationId     = corporationId,
            LibraryId         = libraryId,
            CategoryId        = categoryId,
            DevelopmentAreaId = developmentAreaId,
            Code              = code,
            Statement         = statement,
            DefaultCriteria   = defaultCriteria,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        Guid? libraryId,
        Guid? categoryId,
        Guid? developmentAreaId,
        string? code,
        string statement,
        string? defaultCriteria)
    {
        LibraryId         = libraryId;
        CategoryId        = categoryId;
        DevelopmentAreaId = developmentAreaId;
        Code              = code;
        Statement         = statement;
        DefaultCriteria   = defaultCriteria;
        UpdatedAt         = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets or replaces the translation for a given locale.
    /// Idempotent — calling twice for the same locale updates the existing record.
    /// </summary>
    public GoalTemplateTranslation SetTranslation(
        string locale, string statement, string? defaultCriteria)
    {
        var existing = Translations.FirstOrDefault(t => t.Locale == locale);

        if (existing is not null)
        {
            existing.Statement       = statement;
            existing.DefaultCriteria = defaultCriteria;
            return existing;
        }

        var translation = new GoalTemplateTranslation
        {
            GoalTemplateId  = Id,
            Locale          = locale,
            Statement       = statement,
            DefaultCriteria = defaultCriteria
        };

        Translations.Add(translation);
        return translation;
    }
}
