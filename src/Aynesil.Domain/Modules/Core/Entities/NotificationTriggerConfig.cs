namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.notification_trigger_config.
/// Business-configurable rule: trigger event code → notification template + timing offset.
/// CorporationId nullable: NULL = platform-level default rule; set = tenant override.
/// Channels are stored in the NotificationTriggerChannel junction table.
/// </summary>
public class NotificationTriggerConfig : BaseEntity
{
    /// <summary>NULL = platform default; set = tenant-specific override.</summary>
    public Guid? CorporationId { get; private set; }

    /// <summary>
    /// Stable application event code matched by domain-event handlers.
    /// Examples: 'session_reminder', 'session_completed', 'package_expiring'.
    /// </summary>
    public string TriggerCode { get; private set; } = string.Empty;

    /// <summary>FK to core.notification_template — the template to render for this trigger.</summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>
    /// Signed offset in minutes from the triggering event timestamp.
    /// -1440 = 24 hours before event. 0 = at event time. 60 = 1 hour after event.
    /// </summary>
    public int OffsetMinutes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RowVersion { get; set; } = 1;

    public NotificationTemplate? Template { get; private set; }
    public ICollection<NotificationTriggerChannel> Channels { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NotificationTriggerConfig Create(
        string triggerCode,
        Guid? templateId,
        int offsetMinutes = 0,
        Guid? corporationId = null,
        Guid? createdBy = null)
        => new()
        {
            CorporationId  = corporationId,
            TriggerCode    = triggerCode.Trim().ToLowerInvariant(),
            TemplateId     = templateId,
            OffsetMinutes  = offsetMinutes,
            IsActive       = true,
            CreatedAt      = DateTimeOffset.UtcNow,
            CreatedBy      = createdBy,
            UpdatedAt      = DateTimeOffset.UtcNow
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Update(Guid? templateId, int offsetMinutes, bool isActive)
    {
        TemplateId    = templateId;
        OffsetMinutes = offsetMinutes;
        IsActive      = isActive;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Activate()   { IsActive = true;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
