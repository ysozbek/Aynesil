using Aynesil.Domain.Modules.Education.Events;

namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// An individualized goal for a student, optionally derived from a GoalTemplate.
/// Goals are hierarchical: long-term goals can parent short-term goals (parent_goal_id).
/// Category (goal_category) and DevelopmentArea (development_area) are configurable ref data.
///
/// Status lifecycle: active → achieved | discontinued | on_hold
/// (status values are checked constraints in DDL — not configurable ref data).
///
/// Maps to education.student_goal.
/// Audit: full (created_at, created_by, updated_at, updated_by, deleted_at, row_version).
/// </summary>
public class StudentGoal : TenantEntity
{
    public Guid StudentId { get; private set; }

    /// <summary>FK to education.goal_template — the source template (if cloned from library).</summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'goal_category'). Configurable.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'development_area'). Configurable.</summary>
    public Guid? DevelopmentAreaId { get; private set; }

    /// <summary>long_term | short_term. Checked constraint in DDL.</summary>
    public string Horizon { get; private set; } = "short_term";

    /// <summary>Self-reference: short-term goals can nest under a long-term goal.</summary>
    public Guid? ParentGoalId { get; private set; }

    public string Statement { get; private set; } = string.Empty;

    /// <summary>Mastery criteria — conditions under which the goal is considered achieved.</summary>
    public string? MasteryCriteria { get; private set; }

    /// <summary>Baseline measurement text.</summary>
    public string? Baseline { get; private set; }

    /// <summary>Numeric target value for measurable goals (e.g. 80 for "80% accuracy").</summary>
    public decimal? TargetValue { get; private set; }

    /// <summary>active | achieved | discontinued | on_hold</summary>
    public string Status { get; private set; } = "active";

    public DateOnly? StartDate { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public DateOnly? AchievedDate { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public GoalTemplate? Template { get; private set; }
    public ICollection<GoalProgress> ProgressRecords { get; private set; } = [];
    public ICollection<StudentGoal> ChildGoals { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static StudentGoal Create(
        Guid corporationId,
        Guid studentId,
        string statement,
        string horizon = "short_term",
        Guid? templateId = null,
        Guid? categoryId = null,
        Guid? developmentAreaId = null,
        Guid? parentGoalId = null,
        string? masteryCriteria = null,
        string? baseline = null,
        decimal? targetValue = null,
        DateOnly? startDate = null,
        DateOnly? targetDate = null,
        Guid? createdBy = null)
    {
        ValidateHorizon(horizon);

        var goal = new StudentGoal
        {
            CorporationId     = corporationId,
            StudentId         = studentId,
            TemplateId        = templateId,
            CategoryId        = categoryId,
            DevelopmentAreaId = developmentAreaId,
            Horizon           = horizon,
            ParentGoalId      = parentGoalId,
            Statement         = statement,
            MasteryCriteria   = masteryCriteria,
            Baseline          = baseline,
            TargetValue       = targetValue,
            Status            = "active",
            StartDate         = startDate,
            TargetDate        = targetDate,
            CreatedAt         = DateTimeOffset.UtcNow,
            UpdatedAt         = DateTimeOffset.UtcNow,
            CreatedBy         = createdBy
        };

        goal.AddDomainEvent(new StudentGoalCreatedEvent(
            goal.Id, corporationId, studentId, horizon, templateId, createdBy));

        return goal;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateDetails(
        string statement,
        Guid? categoryId,
        Guid? developmentAreaId,
        string? masteryCriteria,
        string? baseline,
        decimal? targetValue,
        DateOnly? startDate,
        DateOnly? targetDate,
        Guid? updatedBy = null)
    {
        Statement         = statement;
        CategoryId        = categoryId;
        DevelopmentAreaId = developmentAreaId;
        MasteryCriteria   = masteryCriteria;
        Baseline          = baseline;
        TargetValue       = targetValue;
        StartDate         = startDate;
        TargetDate        = targetDate;
        UpdatedAt         = DateTimeOffset.UtcNow;
        UpdatedBy         = updatedBy;
    }

    public void ChangeStatus(string newStatus, DateOnly? achievedDate = null, Guid? updatedBy = null)
    {
        ValidateStatus(newStatus);

        var previous = Status;
        Status    = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        if (newStatus == "achieved")
            AchievedDate = achievedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        AddDomainEvent(new StudentGoalStatusChangedEvent(
            Id, CorporationId, StudentId, previous, newStatus, updatedBy));
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidHorizons = ["long_term", "short_term"];
    private static readonly string[] ValidStatuses  = ["active", "achieved", "discontinued", "on_hold"];

    private static void ValidateHorizon(string horizon)
    {
        if (!ValidHorizons.Contains(horizon))
            throw new ArgumentException($"Invalid goal horizon '{horizon}'. Must be long_term or short_term.");
    }

    private static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException(
                $"Invalid goal status '{status}'. Must be active, achieved, discontinued, or on_hold.");
    }
}
