using System.Text.Json;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;
using CorsairTide.Server.Infrastructure.EventStore;

namespace CorsairTide.Server.Infrastructure.Repositories;

/// <summary>
/// Loads and saves Island aggregates via the event store.
/// Maintains a secondary index file (_player-index.json) so islands can be
/// looked up by player ID without scanning all event streams.
/// </summary>
public sealed class IslandRepository(IEventStore eventStore, string eventStoreBasePath)
    : IIslandRepository
{
    private const string AggregateType = "Island";

    private readonly string _indexPath =
        Path.Combine(eventStoreBasePath, "Island", "_player-index.json");

    private readonly SemaphoreSlim _indexLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── IIslandRepository ────────────────────────────────────────────────

    public async Task<Island?> GetByIdAsync(IslandId id, CancellationToken ct = default)
    {
        var events = await eventStore.LoadAsync(AggregateType, id.Value.ToString(), ct);
        return events.Count == 0 ? null : Island.Reconstitute(events);
    }

    public async Task<Island?> GetByPlayerIdAsync(PlayerId playerId, CancellationToken ct = default)
    {
        var index = await ReadIndexAsync(ct);
        if (!index.TryGetValue(playerId.Value.ToString(), out var islandIdStr))
            return null;

        return await GetByIdAsync(IslandId.From(Guid.Parse(islandIdStr)), ct);
    }

    public async Task SaveAsync(Island island, CancellationToken ct = default)
    {
        var uncommitted = island.UncommittedEvents;
        if (uncommitted.Count == 0) return;

        await eventStore.AppendAsync(AggregateType, island.Id.Value.ToString(), uncommitted, ct);
        island.ClearUncommittedEvents();

        // Update player→island index on first persist
        await _indexLock.WaitAsync(ct);
        try
        {
            var index = await ReadIndexAsync(ct);
            var key   = island.PlayerId.Value.ToString();
            if (!index.ContainsKey(key))
            {
                index[key] = island.Id.Value.ToString();
                Directory.CreateDirectory(Path.GetDirectoryName(_indexPath)!);
                await File.WriteAllTextAsync(
                    _indexPath, JsonSerializer.Serialize(index, JsonOptions), ct);
            }
        }
        finally
        {
            _indexLock.Release();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Dictionary<string, string>> ReadIndexAsync(CancellationToken ct)
    {
        if (!File.Exists(_indexPath)) return [];
        var json = await File.ReadAllTextAsync(_indexPath, ct);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }
}
