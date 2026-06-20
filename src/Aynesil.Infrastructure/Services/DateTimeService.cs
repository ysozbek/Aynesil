using Aynesil.Application.Common.Interfaces;

namespace Aynesil.Infrastructure.Services;

/// <summary>
/// Production implementation of IDateTimeService.
/// Uses DateTimeOffset.UtcNow and TimeZoneInfo for timezone conversions.
/// Timezone IDs follow IANA convention (e.g. 'Europe/Istanbul').
/// On .NET 9, TimeZoneInfo.FindSystemTimeZoneById works with IANA IDs on all platforms.
/// </summary>
public sealed class DateTimeService : IDateTimeService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset ConvertToTenantTime(DateTimeOffset utcTime, string timezone)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        return TimeZoneInfo.ConvertTime(utcTime, tz);
    }

    public DateOnly TodayInTimezone(string timezone)
    {
        var local = ConvertToTenantTime(DateTimeOffset.UtcNow, timezone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
