using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;

namespace CorsairTide.Server.Application.Islands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ResourcesDto(decimal Wood, decimal Gold, decimal Rum, decimal Food);

public record ActiveUpgradeDto(
    string BuildingType,
    int TargetLevel,
    DateTimeOffset CompletesAt,
    double SecondsRemaining);

public record IslandDto(
    Guid IslandId,
    string Name,
    ResourcesDto Resources,
    IReadOnlyDictionary<string, int> Buildings,
    ActiveUpgradeDto? ActiveUpgrade,
    decimal WoodPerHour,
    decimal GoldPerHour,
    decimal RumPerHour,
    decimal FoodPerHour);

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Application-layer service for island use-cases.
/// Each method loads the island, brings it up to date (lazy resource tick +
/// lazy upgrade completion), executes the command, and saves uncommitted events.
/// </summary>
public class IslandService(IIslandRepository islandRepository)
{
    /// <summary>Returns the current state of the player's island, collecting resources first.</summary>
    public async Task<IslandDto?> GetIslandAsync(Guid playerId, CancellationToken ct = default)
    {
        var island = await islandRepository.GetByPlayerIdAsync(PlayerId.From(playerId), ct);
        if (island is null) return null;

        island.BringUpToDate(DateTimeOffset.UtcNow);
        await islandRepository.SaveAsync(island, ct);

        return ToDto(island);
    }

    /// <summary>Starts a building upgrade, returning the updated island state.</summary>
    public async Task<IslandDto> StartUpgradeAsync(
        Guid playerId, string buildingTypeName, CancellationToken ct = default)
    {
        if (!Enum.TryParse<BuildingType>(buildingTypeName, ignoreCase: true, out var buildingType))
            throw new DomainException($"Unknown building type: '{buildingTypeName}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<BuildingType>())}");

        var island = await islandRepository.GetByPlayerIdAsync(PlayerId.From(playerId), ct)
            ?? throw new DomainException("Island not found for this player.");

        var now = DateTimeOffset.UtcNow;
        island.BringUpToDate(now);
        island.StartUpgrade(buildingType, now);
        await islandRepository.SaveAsync(island, ct);

        return ToDto(island);
    }

    // ── Mapping ──────────────────────────────────────────────────────────

    private static IslandDto ToDto(Island island)
    {
        var b   = island.Buildings;
        var now = DateTimeOffset.UtcNow;

        return new IslandDto(
            IslandId: island.Id.Value,
            Name:     island.Name,
            Resources: new ResourcesDto(
                island.Resources.Wood,
                island.Resources.Gold,
                island.Resources.Rum,
                island.Resources.Food),
            Buildings: b.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            ActiveUpgrade: island.ActiveUpgrade is { } u
                ? new ActiveUpgradeDto(
                    u.Type.ToString(),
                    u.TargetLevel,
                    u.CompletesAt,
                    Math.Max(0, (u.CompletesAt - now).TotalSeconds))
                : null,
            WoodPerHour: (decimal)ProductionRates.Sawmill    (b.GetValueOrDefault(BuildingType.Sawmill)),
            GoldPerHour: (decimal)ProductionRates.GoldMine   (b.GetValueOrDefault(BuildingType.GoldMine)),
            RumPerHour:  (decimal)ProductionRates.Distillery (b.GetValueOrDefault(BuildingType.Distillery)),
            FoodPerHour: (decimal)ProductionRates.FishingDock(b.GetValueOrDefault(BuildingType.FishingDock)));
    }
}
