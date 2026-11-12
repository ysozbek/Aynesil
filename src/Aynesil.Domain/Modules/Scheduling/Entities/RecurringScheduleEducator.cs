using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// An educator assigned to a recurring schedule. Copied to session_educator for
/// every session generated from the rule.
/// Unique constraint: (recurring_schedule_id, educator_id).
///
/// Maps to scheduling.recurring_schedule_educator.
/// No audit fields in DDL — lifecycle managed via cascade delete from the rule.
/// </summary>
public class RecurringScheduleEducator : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid RecurringScheduleId { get; private set; }
    public Guid EducatorId { get; private set; }

    /// <summary>lead | assistant | observer | supervisor</summary>
    public string Role { get; private set; } = "lead";

    // ── Factory ───────────────────────────────────────────────────────────────

    public static RecurringScheduleEducator Assign(
        Guid corporationId,
        Guid recurringScheduleId,
        Guid educatorId,
        string role = "lead")
    {
        ValidateRole(role);

        return new RecurringScheduleEducator
        {
            CorporationId       = corporationId,
            RecurringScheduleId = recurringScheduleId,
            EducatorId          = educatorId,
            Role                = role
        };
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    private static void ValidateRole(string role)
    {
        if (!ValidRoles.Contains(role))
            throw new InvalidArgumentException(MsgKeys.Validation.MustBeOneOf, MsgArg.Field("Role"), MsgArg.Values("EducatorRole", "lead", "assistant", "observer", "supervisor"));
    }
}
