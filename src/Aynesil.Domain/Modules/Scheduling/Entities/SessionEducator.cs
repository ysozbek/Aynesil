using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// An educator assigned to a session.
/// Unique constraint: (session_id, educator_id).
/// Educator double-booking is enforced by a DB trigger (see 99_triggers_rls_policies.sql).
///
/// Maps to scheduling.session_educator.
/// No audit fields in DDL — lifecycle managed via cascade delete from session.
/// </summary>
public class SessionEducator : BaseEntity
{
    public Guid CorporationId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid EducatorId { get; private set; }

    /// <summary>lead | assistant | observer | supervisor</summary>
    public string Role { get; private set; } = "lead";

    // ── Navigations ───────────────────────────────────────────────────────────

    public Session? Session { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionEducator Assign(
        Guid corporationId,
        Guid sessionId,
        Guid educatorId,
        string role = "lead")
    {
        ValidateRole(role);

        return new SessionEducator
        {
            CorporationId = corporationId,
            SessionId     = sessionId,
            EducatorId    = educatorId,
            Role          = role
        };
    }

    // ── Invariants ────────────────────────────────────────────────────────────

    private static readonly string[] ValidRoles = ["lead", "assistant", "observer", "supervisor"];

    private static void ValidateRole(string role)
    {
        if (!ValidRoles.Contains(role))
            throw new ArgumentException(
                $"Invalid educator role '{role}'. Must be lead, assistant, observer, or supervisor.");
    }
}
