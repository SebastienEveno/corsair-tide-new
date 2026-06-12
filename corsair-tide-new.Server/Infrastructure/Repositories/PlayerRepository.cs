using System.Text.Json;
using CorsairTide.Server.Domain.Players;
using CorsairTide.Server.Infrastructure.EventStore;

namespace CorsairTide.Server.Infrastructure.Repositories;

/// <summary>
/// Loads and saves Player aggregates via the event store.
/// Maintains a secondary index (_username-index.json) for uniqueness checks.
/// </summary>
public sealed class PlayerRepository(IEventStore eventStore, string eventStoreBasePath)
    : IPlayerRepository
{
    private const string AggregateType = "Player";

    private readonly string _usernameIndexPath =
        Path.Combine(eventStoreBasePath, "Player", "_username-index.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── IPlayerRepository ────────────────────────────────────────────────

    public async Task<Player?> GetByIdAsync(PlayerId id, CancellationToken ct = default)
    {
        var events = await eventStore.LoadAsync(AggregateType, id.Value.ToString(), ct);
        return events.Count == 0 ? null : Player.Reconstitute(events);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        var index = await ReadUsernameIndexAsync(ct);
        return index.ContainsKey(username.ToLowerInvariant());
    }

    public async Task SaveAsync(Player player, CancellationToken ct = default)
    {
        var uncommitted = player.UncommittedEvents;
        if (uncommitted.Count == 0) return;

        await eventStore.AppendAsync(AggregateType, player.Id.Value.ToString(), uncommitted, ct);
        player.ClearUncommittedEvents();

        // Keep username index up to date
        var index = await ReadUsernameIndexAsync(ct);
        index[player.Username.ToLowerInvariant()] = player.Id.Value.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(_usernameIndexPath)!);
        await File.WriteAllTextAsync(
            _usernameIndexPath, JsonSerializer.Serialize(index, JsonOptions), ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Dictionary<string, string>> ReadUsernameIndexAsync(CancellationToken ct)
    {
        if (!File.Exists(_usernameIndexPath)) return [];
        var json = await File.ReadAllTextAsync(_usernameIndexPath, ct);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }
}
