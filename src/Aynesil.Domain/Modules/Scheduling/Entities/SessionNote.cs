using Aynesil.Domain.Common;
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

    /// <summary>
    /// Student the note is about (a session participant; V56). The guardian portal shows a parent-visible
    /// note only to that student's guardians; null + parent_visible is never shown.
    /// </summary>
    public Guid? StudentId { get; private set; }

    /// <summary>Last time the note's text was changed (V55); null = never edited.</summary>
    public DateTimeOffset? EditedAt { get; private set; }

    /// <summary>iam.user_account.id of the user who last changed the text.</summary>
    public Guid? EditedBy { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SessionNote Write(
        Guid corporationId,
        Guid sessionId,
        string body,
        bool parentVisible = false,
        Guid? authoredBy = null,
        Guid? studentId = null)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Body"));

        return new SessionNote
        {
            CorporationId = corporationId,
            SessionId     = sessionId,
            AuthoredBy    = authoredBy,
            StudentId     = studentId,
            Body          = body,
            ParentVisible = parentVisible,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>Metin değişirse edited_at/edited_by dolar; yalnızca görünürlük değişimi düzenleme sayılmaz.</summary>
    public void Edit(string body, bool parentVisible, Guid? studentId, Guid? editedBy = null)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidArgumentException(MsgKeys.Validation.Required, MsgArg.Field("Body"));

        if (!string.Equals(Body, body, StringComparison.Ordinal))
        {
            EditedAt = DateTimeOffset.UtcNow;
            EditedBy = editedBy;
        }

        Body          = body;
        ParentVisible = parentVisible;
        StudentId     = studentId;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }
}
