namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Background job abstraction over Hangfire.
/// Decouples business code from the job scheduler implementation.
/// Future migration path: replace Hangfire with a distributed queue (SQS, Azure Service Bus)
/// by implementing this interface with a message-based dispatcher.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>Enqueue a fire-and-forget job to run immediately in the background.</summary>
    string Enqueue<T>(System.Linq.Expressions.Expression<Action<T>> methodCall);

    string Enqueue<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall);

    /// <summary>Schedule a job to run after a delay.</summary>
    string Schedule<T>(System.Linq.Expressions.Expression<Action<T>> methodCall, TimeSpan delay);

    string Schedule<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>Add or update a recurring cron job.</summary>
    void AddOrUpdateRecurring<T>(
        string jobId,
        System.Linq.Expressions.Expression<Action<T>> methodCall,
        string cronExpression,
        string? timeZone = null);
}
