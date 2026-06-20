namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_template_translation. Composite PK (template_id, locale).
/// Localized subject and body for a notification template.
/// Supports Mustache/Handlebars-style variable substitution in the body.
/// </summary>
public class NotificationTemplateTranslation
{
    public Guid TemplateId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    public NotificationTemplate? Template { get; set; }
}
