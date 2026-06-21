namespace Aynesil.Domain.Modules.Scheduling.Entities;

/// <summary>
/// A narrative note for a session, authored by an educator.
/// parent_visible = true surfaces the note in the guardian portal.
///
/// Maps to scheduling.session_note.
/// Audit: created_at, updated_at, deleted_at, row_version (no created_by / updated_by in DDL).
/// </summary>
public class SessionNote : TenantEntity
{
    public Guid SessionId { get; private set; }

    /// <summary>FK to educators.educator.id — the educator who authored this note.</summary>
    public Guid? AuthoredBy { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>When true the note is visible in the guardian/parent portal.</summary>
    public bool ParentVisible { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionNote Write(
        Guid corporationId,
        Guid sessionId,
        string body,
        bool parentVisible = false,
        Guid? authoredBy = null)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Session note body cannot be empty.");

        return new SessionNote
        {
            CorporationId = corporationId,
            SessionId     = sessionId,
            AuthoredBy    = authoredBy,
            Body          = body,
            ParentVisible = parentVisible,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Edit(string body, bool parentVisible)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Session note body cannot be empty.");

        Body          = body;
        ParentVisible = parentVisible;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }
}
