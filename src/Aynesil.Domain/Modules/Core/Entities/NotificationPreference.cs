namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_preference.
/// Per-user opt-in/opt-out for notification category × channel combinations.
/// Defaults to enabled (IsEnabled = true). Users manage via their profile settings.
/// </summary>
public class NotificationPreference : TenantEntity
{
    public Guid UserId { get; set; }

    /// <summary>FK to ref_value(notification_category). Null = all categories.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>FK to ref_value(notification_channel). Null = all channels.</summary>
    public Guid? ChannelId { get; set; }

    public bool IsEnabled { get; set; } = true;
}
