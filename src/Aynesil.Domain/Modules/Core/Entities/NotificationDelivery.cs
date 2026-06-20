namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_delivery.
/// Tracks the per-channel delivery attempt for a notification.
/// One AppNotification can have multiple deliveries (in-app, email, SMS, push).
/// Status: queued → sent → delivered (or failed/bounced).
/// </summary>
public class NotificationDelivery : BaseEntity
{
    public Guid NotificationId { get; set; }

    /// <summary>FK to ref_value(notification_channel): 'email', 'sms', 'push', 'in_app'.</summary>
    public Guid? ChannelId { get; set; }

    /// <summary>FK to core.integration_connection used for dispatch.</summary>
    public Guid? ProviderId { get; set; }

    /// <summary>'queued', 'sent', 'delivered', 'failed', 'bounced'.</summary>
    public string Status { get; set; } = "queued";

    public int Attempts { get; set; }

    public string? ErrorDetail { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public AppNotification? Notification { get; set; }
}
