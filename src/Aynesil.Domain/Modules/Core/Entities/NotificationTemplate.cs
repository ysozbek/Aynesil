namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_template.
/// Reusable templates for system-generated notifications.
/// CorporationId nullable: NULL = platform default template; set = tenant custom override.
/// Each template has localized subject/body in notification_template_translation.
/// </summary>
public class NotificationTemplate : BaseEntity
{
    /// <summary>NULL = platform default; set = tenant-specific template.</summary>
    public Guid? CorporationId { get; set; }

    /// <summary>Stable machine code, e.g. 'session_reminder', 'invoice_created'.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>FK to ref_value(notification_category).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>FK to ref_value(notification_type).</summary>
    public Guid? TypeId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public ICollection<NotificationTemplateTranslation> Translations { get; set; } = [];
}
