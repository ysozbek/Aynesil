using System.Linq.Expressions;
using Aynesil.Application.Common.Interfaces;
using Hangfire;

namespace Aynesil.Infrastructure.Services.Jobs;

/// <summary>
/// Hangfire implementation of IBackgroundJobService.
/// Hangfire stores job state in PostgreSQL (same database, core schema).
/// Dashboard is mounted at /hangfire (access restricted to Admin role in production).
/// </summary>
public sealed class HangfireJobService : IBackgroundJobService
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(
        string jobId,
        Expression<Action<T>> methodCall,
        string cronExpression,
        string? timeZone = null)
    {
        var tz = timeZone is not null
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            : TimeZoneInfo.Utc;
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression, tz);
    }
}
