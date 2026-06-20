namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification.
/// A concrete notification instance sent to a specific user.
/// Named AppNotification in C# to avoid conflict with System.Notification.
/// Status transitions: pending → sent → read (or failed/cancelled).
/// </summary>
public class AppNotification : TenantEntity
{
    public Guid? TemplateId { get; set; }

    /// <summary>FK to ref_value(notification_category).</summary>
    public Guid? CategoryId { get; set; }

    public Guid? RecipientUserId { get; set; }

    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    /// <summary>Template variable substitution data as JSON.</summary>
    public string Payload { get; set; } = "{}";

    /// <summary>'pending', 'sent', 'read', 'failed', 'cancelled'.</summary>
    public string Status { get; set; } = "pending";

    public DateTimeOffset? ReadAt { get; set; }

    public NotificationTemplate? Template { get; set; }
    public ICollection<NotificationDelivery> Deliveries { get; set; } = [];
}
