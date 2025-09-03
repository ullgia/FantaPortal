namespace Application.Events;

public class InMemoryDomainEventPublisher : IDomainEventPublisher
{
    public event EventHandler<object>? EventPublished;

    public void Publish(object @event)
        => EventPublished?.Invoke(this, @event);
}
