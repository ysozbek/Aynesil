namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.activity_log. Append-only. Bigint identity PK.
/// Written by the application layer for user activity: login, view, export, download.
/// Range-partitioned by occurred_at.
/// </summary>
public class ActivityLog
{
    public long Id { get; set; }

    public Guid? CorporationId { get; set; }

    public Guid? UserId { get; set; }

    /// <summary>Activity type: 'login', 'view', 'export', 'download', etc.</summary>
    public string ActivityType { get; set; } = string.Empty;

    public string? TargetSchema { get; set; }
    public string? TargetTable { get; set; }
    public Guid? TargetId { get; set; }

    /// <summary>Additional context as JSON (filter params, IP, browser, etc.).</summary>
    public string Context { get; set; } = "{}";

    public string? IpAddress { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
