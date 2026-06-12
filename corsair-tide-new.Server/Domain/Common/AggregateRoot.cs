namespace CorsairTide.Server.Domain.Common;

/// <summary>
/// Base class for all aggregate roots.
///
/// - Use <see cref="Raise"/> inside the aggregate to record and immediately
///   apply a new domain event.
/// - Use <see cref="Load"/> to reconstitute state by replaying a historical
///   event stream (called by the repository, never by application code).
/// - Call <see cref="ClearUncommittedEvents"/> after persisting to the event
///   store so the same events are not appended twice.
/// </summary>
public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<DomainEvent> _uncommittedEvents = [];

    public TId Id { get; protected set; } = default!;

    /// <summary>Current version = number of events applied so far.</summary>
    public int Version { get; private set; }

    public IReadOnlyList<DomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>Raises a new event: applies it to state, increments version, queues for persistence.</summary>
    protected void Raise(DomainEvent @event)
    {
        Apply(@event);
        Version++;
        _uncommittedEvents.Add(@event);
    }

    /// <summary>Replays persisted events to rebuild aggregate state. Does not add to uncommitted events.</summary>
    public void Load(IEnumerable<DomainEvent> history)
    {
        foreach (var @event in history)
        {
            Apply(@event);
            Version++;
        }
    }

    protected abstract void Apply(DomainEvent @event);

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}
