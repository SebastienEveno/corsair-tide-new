namespace CorsairTide.Server.Domain.Islands;

// ── Building types ────────────────────────────────────────────────────────────

public enum BuildingType
{
    // Producers
    Sawmill,
    GoldMine,
    Distillery,
    FishingDock,
    // Storage
    WoodStorage,
    GoldStorage,
    RumStorage,
    FoodStorage
}

/// <summary>Snapshot of an in-progress building upgrade.</summary>
public record BuildingUpgrade(BuildingType Type, int TargetLevel, DateTimeOffset CompletesAt);

// ── Production rates (per hour, by building level) ────────────────────────────

/// <summary>
/// OGame-style exponential production curves.
/// Level 0 means the building hasn't been built yet → zero output.
/// </summary>
public static class ProductionRates
{
    public static double Sawmill(int level)    => level == 0 ? 0 : 30  * Math.Pow(1.5, level);
    public static double GoldMine(int level)   => level == 0 ? 0 : 20  * Math.Pow(1.5, level);
    public static double Distillery(int level) => level == 0 ? 0 : 10  * Math.Pow(1.5, level);
    public static double FishingDock(int level)=> level == 0 ? 0 : 15  * Math.Pow(1.4, level);
}

// ── Storage capacities ────────────────────────────────────────────────────────

public static class StorageCapacities
{
    private const decimal Base = 1_000m;

    /// <summary>Capacity for a storage building at <paramref name="level"/>.</summary>
    public static decimal ForLevel(int level) => Base * (decimal)Math.Pow(2, level);

    /// <summary>Returns the effective storage caps given the current building levels.</summary>
    public static Resources Get(IReadOnlyDictionary<BuildingType, int> buildings) => new(
        Wood: ForLevel(buildings.GetValueOrDefault(BuildingType.WoodStorage)),
        Gold: ForLevel(buildings.GetValueOrDefault(BuildingType.GoldStorage)),
        Rum:  ForLevel(buildings.GetValueOrDefault(BuildingType.RumStorage)),
        Food: ForLevel(buildings.GetValueOrDefault(BuildingType.FoodStorage)));
}

// ── Building upgrade costs & durations ───────────────────────────────────────

public static class BuildingCosts
{
    private static Resources BaseCost(BuildingType type) => type switch
    {
        BuildingType.Sawmill     => new Resources(60,  15,  0, 0),
        BuildingType.GoldMine    => new Resources(15,  30,  0, 0),
        BuildingType.Distillery  => new Resources(20,  40,  0, 0),
        BuildingType.FishingDock => new Resources(10,  20,  5, 0),
        BuildingType.WoodStorage => new Resources(100,  0,  0, 0),
        BuildingType.GoldStorage => new Resources(50, 100,  0, 0),
        BuildingType.RumStorage  => new Resources(75,  75,  0, 0),
        BuildingType.FoodStorage => new Resources(40,  50,  0, 0),
        _                        => Resources.Zero
    };

    /// <summary>Returns the resource cost to upgrade to <paramref name="targetLevel"/>.</summary>
    public static Resources UpgradeCost(BuildingType type, int targetLevel)
    {
        var base_ = BaseCost(type);
        var m = (decimal)Math.Pow(1.8, targetLevel - 1);
        return new Resources(
            Wood: Math.Floor(base_.Wood * m),
            Gold: Math.Floor(base_.Gold * m),
            Rum:  Math.Floor(base_.Rum  * m),
            Food: Math.Floor(base_.Food * m));
    }

    /// <summary>
    /// Returns how long the upgrade takes. Starts at 30 s for level 1,
    /// multiplied by 2.5 for each subsequent level.
    /// </summary>
    public static TimeSpan UpgradeDuration(BuildingType type, int targetLevel) =>
        TimeSpan.FromSeconds(30 * Math.Pow(2.5, targetLevel - 1));
}
