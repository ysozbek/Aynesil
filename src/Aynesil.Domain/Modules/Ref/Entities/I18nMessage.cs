namespace Aynesil.Domain.Modules.Ref.Entities;

/// <summary>
/// Maps to ref.i18n_message.
/// Admin-managed key/value catalog for static UI and system string translations.
/// Namespace examples: 'ui.menu', 'validation', 'email.subject'.
/// NOT for entity content (that uses per-entity *_translation sidecar tables).
/// </summary>
public class I18nMessage : BaseEntity
{
    /// <summary>Grouping namespace, e.g. 'ui.menu', 'validation', 'notification'.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Message key within the namespace, e.g. 'dashboard.title'.</summary>
    public string MsgKey { get; set; } = string.Empty;

    /// <summary>BCP-47 locale code. FK to ref.locale.</summary>
    public string Locale { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int RowVersion { get; set; } = 1;

    public Locale? LocaleNavigation { get; set; }
}
