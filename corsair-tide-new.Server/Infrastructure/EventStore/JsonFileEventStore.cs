using System.Text.Json;
using System.Text.Json.Serialization;
using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.Infrastructure.EventStore;

/// <summary>
/// Local-development event store that persists events as JSON files.
///
/// Layout on disk:
/// <code>
///   {basePath}/
///     Island/
///       {islandId}.json      ← array of StoredEvent
///       _player-index.json   ← managed by IslandRepository
///     Player/
///       {playerId}.json
///       _username-index.json ← managed by PlayerRepository
/// </code>
///
/// Swap this for an EventStoreDB or PostgreSQL implementation in production
/// by registering a different <see cref="IEventStore"/> in DI.
/// </summary>
public sealed class JsonFileEventStore : IEventStore
{
    private readonly string _basePath;

    // One lock per aggregate stream (keyed by "type/id") prevents concurrent
    // writes to the same file, while allowing parallel writes to different streams.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
        _locks = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonFileEventStore(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    // ── IEventStore ──────────────────────────────────────────────────────

    public async Task AppendAsync(
        string aggregateType, string aggregateId,
        IEnumerable<DomainEvent> events, CancellationToken ct = default)
    {
        var key  = $"{aggregateType}/{aggregateId}";
        var sem  = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await sem.WaitAsync(ct);
        try
        {
            var existing    = await ReadStoredEventsAsync(aggregateType, aggregateId, ct);
            var nextVersion = existing.Count > 0 ? existing[^1].Version + 1 : 1;

            var toAppend = events.Select((e, i) => new StoredEvent(
                EventId:    e.EventId.ToString(),
                EventType:  e.GetType().Name,
                Version:    nextVersion + i,
                OccurredAt: e.OccurredAt,
                Payload:    JsonSerializer.Serialize(e, e.GetType(), JsonOptions)));

            existing.AddRange(toAppend);

            var path = GetFilePath(aggregateType, aggregateId);
            await File.WriteAllTextAsync(
                path, JsonSerializer.Serialize(existing, JsonOptions), ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<IReadOnlyList<DomainEvent>> LoadAsync(
        string aggregateType, string aggregateId, CancellationToken ct = default)
    {
        var stored = await ReadStoredEventsAsync(aggregateType, aggregateId, ct);
        return stored
            .Select(Deserialize)
            .OfType<DomainEvent>()
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<List<StoredEvent>> ReadStoredEventsAsync(
        string aggregateType, string aggregateId, CancellationToken ct)
    {
        var path = GetFilePath(aggregateType, aggregateId);
        if (!File.Exists(path)) return [];

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<List<StoredEvent>>(json, JsonOptions) ?? [];
    }

    private static DomainEvent? Deserialize(StoredEvent stored)
    {
        var type = EventTypeRegistry.Resolve(stored.EventType);
        if (type is null) return null;
        return (DomainEvent?)JsonSerializer.Deserialize(stored.Payload, type, JsonOptions);
    }

    private string GetFilePath(string aggregateType, string aggregateId)
    {
        var dir = Path.Combine(_basePath, aggregateType);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{aggregateId}.json");
    }
}
