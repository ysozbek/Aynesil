namespace Aynesil.Domain.Modules.Ops.Entities;

/// <summary>
/// Maps to ops.survey_definition.
/// Configurable multi-question survey / feedback form header, scoped to one tenant.
/// target: 'guardian' = parent portal, 'educator' = internal staff, 'student' = direct.
/// TypeId references ref_value(survey_type): 'satisfaction_survey', 'session_feedback', etc.
/// </summary>
public class SurveyDefinition : TenantEntity
{
    /// <summary>FK to ref_value(survey_type) — configurable, not hardcoded.</summary>
    public Guid? TypeId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>'guardian' | 'educator' | 'student'.</summary>
    public string Target { get; private set; } = "guardian";

    public bool IsActive { get; private set; } = true;

    public ICollection<SurveyQuestion> Questions { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SurveyDefinition Create(
        Guid corporationId,
        string title,
        string target = "guardian",
        Guid? typeId = null,
        string? description = null,
        Guid? createdBy = null)
        => new()
        {
            CorporationId = corporationId,
            Title         = title.Trim(),
            Target        = target,
            TypeId        = typeId,
            Description   = description,
            IsActive      = true,
            CreatedBy     = createdBy,
            UpdatedBy     = createdBy
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(string title, string? description, string target, Guid? typeId, bool isActive)
    {
        Title       = title.Trim();
        Description = description;
        Target      = target;
        TypeId      = typeId;
        IsActive    = isActive;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }

    public void Activate()   { IsActive = true;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
