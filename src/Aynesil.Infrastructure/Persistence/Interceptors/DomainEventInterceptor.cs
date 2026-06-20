using Aynesil.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aynesil.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dispatches domain events via MediatR AFTER the transaction successfully commits.
/// Events are collected from all entities in the change tracker, cleared from the entity,
/// then published once SaveChanges completes successfully.
///
/// Future distributed scenario: events that cross process boundaries are also written
/// to core.outbox_event WITHIN the same transaction. The outbox processor picks them
/// up independently for reliable external dispatch.
/// </summary>
public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventInterceptor> _logger;

    public DomainEventInterceptor(IMediator mediator, ILogger<DomainEventInterceptor> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEventsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null) return;

        var domainEventEntities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = domainEventEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        domainEventEntities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug(
                "Dispatching domain event {EventType} ({EventId})",
                domainEvent.GetType().Name, domainEvent.EventId);

            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
