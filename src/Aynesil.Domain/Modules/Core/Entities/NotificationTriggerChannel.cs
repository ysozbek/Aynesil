namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_trigger_channel.
/// Junction: one NotificationTriggerConfig → many delivery channels.
/// ChannelId references ref.ref_value(notification_channel): 'email', 'sms', 'push', 'in_app'.
/// Adding a new channel requires only an INSERT — no schema change.
/// </summary>
public class NotificationTriggerChannel : BaseEntity
{
    public Guid TriggerConfigId { get; private set; }

    /// <summary>FK to ref_value(notification_channel).</summary>
    public Guid ChannelId { get; private set; }

    public NotificationTriggerConfig? TriggerConfig { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NotificationTriggerChannel Create(Guid triggerConfigId, Guid channelId)
        => new()
        {
            TriggerConfigId = triggerConfigId,
            ChannelId       = channelId
        };
}
