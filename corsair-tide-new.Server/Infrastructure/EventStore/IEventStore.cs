using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.Infrastructure.EventStore;

/// <summary>
/// Append-only event store abstraction.
/// For local development the implementation writes JSON files to disk.
/// Swap in EventStoreDB, PostgreSQL+JSONB, or any other backend later.
/// </summary>
public interface IEventStore
{
    /// <summary>Appends <paramref name="events"/> to the stream for the given aggregate.</summary>
    Task AppendAsync(
        string aggregateType,
        string aggregateId,
        IEnumerable<DomainEvent> events,
        CancellationToken ct = default);

    /// <summary>Loads the full event stream for the given aggregate (oldest first).</summary>
    Task<IReadOnlyList<DomainEvent>> LoadAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken ct = default);
}
