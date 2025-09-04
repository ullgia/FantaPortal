using Application.Events;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventPublisher _publisher;

    public DomainEventInterceptor(IDomainEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEvents(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, 
        int result, 
        CancellationToken cancellationToken = default)
    {
        DispatchDomainEvents(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void DispatchDomainEvents(DbContext? context)
    {
        if (context == null) return;

        var aggregates = context.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<AggregateRoot>()
            .ToList();

        foreach (var agg in aggregates)
        {
            foreach (var @event in agg.DomainEvents)
                _publisher.Publish(@event);
            agg.ClearDomainEvents();
        }
    }
}
