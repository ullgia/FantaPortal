namespace Application.Events;

public interface IDomainEventPublisher
{
    event EventHandler<object>? EventPublished;
    void Publish(object @event);
}
