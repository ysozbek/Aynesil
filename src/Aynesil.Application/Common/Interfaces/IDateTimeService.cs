namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Abstraction over DateTimeOffset.UtcNow to enable deterministic testing.
/// All domain/application code must use this service instead of DateTime.Now.
/// Timezone conversions use the corporation's configured timezone (Europe/Istanbul default).
/// </summary>
public interface IDateTimeService
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset ConvertToTenantTime(DateTimeOffset utcTime, string timezone);

    DateOnly TodayInTimezone(string timezone);
}
