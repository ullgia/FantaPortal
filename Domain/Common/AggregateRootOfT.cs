namespace Domain.Common;

public abstract class AggregateRoot<TId> : BaseEntity<TId>
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents;

    protected void RaiseDomainEvent(object @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
