namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Links a StudentGoal to an EducationPlan, classifying it as a long-term or short-term goal
/// within that plan and providing an ordering hint.
/// Unique constraint: (education_plan_id, student_goal_id) — a goal appears once per plan.
/// Maps to education.education_plan_goal.
/// No audit columns in DDL.
/// </summary>
public class EducationPlanGoal : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid EducationPlanId { get; private set; }
    public Guid StudentGoalId { get; private set; }

    /// <summary>long_term | short_term. Checked constraint in DDL.</summary>
    public string Horizon { get; private set; } = "short_term";

    public int SortOrder { get; private set; }

    // ── Navigations ───────────────────────────────────────────────────────────

    public EducationPlan Plan { get; private set; } = null!;
    public StudentGoal Goal { get; private set; } = null!;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducationPlanGoal Create(
        Guid corporationId,
        Guid educationPlanId,
        Guid studentGoalId,
        string horizon = "short_term",
        int sortOrder = 0)
    {
        if (!new[] { "long_term", "short_term" }.Contains(horizon))
            throw new ArgumentException($"Invalid horizon '{horizon}'. Must be long_term or short_term.");

        return new EducationPlanGoal
        {
            CorporationId   = corporationId,
            EducationPlanId = educationPlanId,
            StudentGoalId   = studentGoalId,
            Horizon         = horizon,
            SortOrder       = sortOrder
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}
