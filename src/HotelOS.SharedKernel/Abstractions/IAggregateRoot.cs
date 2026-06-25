// An Aggregate Root is the only entry point for modifying a cluster of domain objects.
// It collects domain events raised during state changes and they are dispatched after
// the database transaction commits — ensuring events only fire on successful persistence.
namespace HotelOS.SharedKernel.Abstractions;

public abstract class AggregateRoot<TId> : IEntity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
