using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// Records which student goals were addressed during a session and any inline progress note.
/// For detailed measurement series use education.goal_progress with session_id.
///
/// Maps to scheduling.session_goal.
/// No audit fields in DDL — lifecycle managed via cascade delete from session.
/// </summary>
public class SessionGoal : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid StudentGoalId { get; private set; }
    public bool WorkedOn { get; private set; } = true;
    public string? ProgressNote { get; private set; }

    /// <summary>Optional numeric measurement captured at session time.</summary>
    public decimal? MeasuredValue { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionGoal Create(
        Guid corporationId,
        Guid sessionId,
        Guid studentGoalId,
        bool workedOn = true,
        string? progressNote = null,
        decimal? measuredValue = null)
        => new()
        {
            CorporationId  = corporationId,
            SessionId      = sessionId,
            StudentGoalId  = studentGoalId,
            WorkedOn       = workedOn,
            ProgressNote   = progressNote,
            MeasuredValue  = measuredValue
        };

    public void Update(bool workedOn, string? progressNote, decimal? measuredValue)
    {
        WorkedOn      = workedOn;
        ProgressNote  = progressNote;
        MeasuredValue = measuredValue;
    }
}
