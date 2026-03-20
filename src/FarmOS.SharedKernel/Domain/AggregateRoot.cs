namespace FarmOS.SharedKernel;

/// <summary>
/// Base class for all aggregate roots in the system.
/// Collects uncommitted domain events and supports rehydration from event history.
/// </summary>
public abstract class AggregateRoot<TId> where TId : notnull
{
    public TenantId TenantId { get; protected set; } = TenantId.Sovereign;
    public TId Id { get; protected set; } = default!;
    public int Version { get; protected set; }

    private readonly List<IDomainEvent> _uncommittedEvents = [];

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    protected void RaiseEvent(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    public void ClearEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Each aggregate must implement event routing to mutate its own state.
    /// </summary>
    protected abstract void Apply(IDomainEvent @event);

    /// <summary>
    /// Replays a stream of historical events to rebuild aggregate state.
    /// </summary>
    public void Rehydrate(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
        {
            Apply(e);
            Version++;
        }
    }
}
