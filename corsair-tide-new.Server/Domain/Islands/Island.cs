using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands.Events;
using CorsairTide.Server.Domain.Players;

namespace CorsairTide.Server.Domain.Islands;

/// <summary>
/// Island aggregate root.
///
/// Key design decisions:
/// - Resources are calculated **lazily**: no background ticker. Every time
///   <see cref="BringUpToDate"/> is called (on each API read), the elapsed
///   time since <see cref="LastResourceUpdate"/> is used to compute
///   accumulated production. The result is recorded as a domain event.
/// - Building upgrade completion is also lazy: <see cref="BringUpToDate"/>
///   checks whether the active upgrade has passed its completion time and,
///   if so, emits <see cref="BuildingUpgradeCompletedEvent"/> before
///   calculating resource production with the new building level.
/// </summary>
public class Island : AggregateRoot<IslandId>
{
    private Dictionary<BuildingType, int> _buildings = [];

    private Island() { }

    public PlayerId PlayerId { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public Resources Resources { get; private set; } = Resources.Zero;
    public BuildingUpgrade? ActiveUpgrade { get; private set; }
    public DateTimeOffset LastResourceUpdate { get; private set; }

    public IReadOnlyDictionary<BuildingType, int> Buildings => _buildings;

    // ── Factories ────────────────────────────────────────────────────────

    // Starter resources — enough to upgrade any building immediately for testing.
    private static readonly Resources StarterResources = new(Wood: 500, Gold: 300, Rum: 100, Food: 50);

    public static Island Create(PlayerId playerId, string name)
    {
        var island = new Island();
        island.Raise(new IslandCreatedEvent(Guid.NewGuid(), playerId.Value, name, StarterResources));
        return island;
    }

    public static Island Reconstitute(IEnumerable<DomainEvent> history)
    {
        var island = new Island();
        island.Load(history);
        return island;
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Brings the island fully up to date as of <paramref name="now"/>.
    /// Order matters: complete any finished upgrades first so the new
    /// building level is factored into resource production.
    /// </summary>
    public void BringUpToDate(DateTimeOffset now)
    {
        CompleteFinishedUpgrade(now);
        CollectAccumulatedResources(now);
    }

    /// <summary>Queues a building upgrade, deducting the cost from current resources.</summary>
    public void StartUpgrade(BuildingType buildingType, DateTimeOffset now)
    {
        // Ensure state is current before validating
        CompleteFinishedUpgrade(now);

        if (ActiveUpgrade is not null)
            throw new DomainException("An upgrade is already in progress.");

        var currentLevel = _buildings.GetValueOrDefault(buildingType);
        var targetLevel  = currentLevel + 1;
        var cost         = BuildingCosts.UpgradeCost(buildingType, targetLevel);

        if (!Resources.CanAfford(cost))
            throw new DomainException(
                $"Not enough resources to upgrade {buildingType} to level {targetLevel}. " +
                $"Needed — Wood: {cost.Wood}, Gold: {cost.Gold}, Rum: {cost.Rum}, Food: {cost.Food}.");

        var duration = BuildingCosts.UpgradeDuration(buildingType, targetLevel);

        Raise(new BuildingUpgradeStartedEvent(
            Id.Value, buildingType, currentLevel, targetLevel,
            StartsAt: now, CompletesAt: now + duration, Cost: cost));
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void CompleteFinishedUpgrade(DateTimeOffset now)
    {
        if (ActiveUpgrade is { } u && u.CompletesAt <= now)
            Raise(new BuildingUpgradeCompletedEvent(
                Id.Value, u.Type, u.TargetLevel, u.CompletesAt));
    }

    private void CollectAccumulatedResources(DateTimeOffset now)
    {
        var elapsed = now - LastResourceUpdate;
        if (elapsed.TotalSeconds < 1) return;

        var produced = CalculateProduction(elapsed);
        if (produced == Resources.Zero) return;

        Raise(new ResourcesCollectedEvent(Id.Value, produced, now));
    }

    private Resources CalculateProduction(TimeSpan elapsed)
    {
        var hours    = elapsed.TotalHours;
        var capacity = StorageCapacities.Get(_buildings);

        var raw = new Resources(
            Wood: (decimal)(ProductionRates.Sawmill    (_buildings.GetValueOrDefault(BuildingType.Sawmill))     * hours),
            Gold: (decimal)(ProductionRates.GoldMine   (_buildings.GetValueOrDefault(BuildingType.GoldMine))    * hours),
            Rum:  (decimal)(ProductionRates.Distillery (_buildings.GetValueOrDefault(BuildingType.Distillery))  * hours),
            Food: (decimal)(ProductionRates.FishingDock(_buildings.GetValueOrDefault(BuildingType.FishingDock)) * hours));

        // Cap production at the remaining storage space (not total capacity)
        var headroom = new Resources(
            Wood: Math.Max(0, capacity.Wood - Resources.Wood),
            Gold: Math.Max(0, capacity.Gold - Resources.Gold),
            Rum:  Math.Max(0, capacity.Rum  - Resources.Rum),
            Food: Math.Max(0, capacity.Food - Resources.Food));

        return raw.CapAt(headroom);
    }

    // ── Event application ────────────────────────────────────────────────

    protected override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case IslandCreatedEvent e:
                Id                 = IslandId.From(e.IslandId);
                PlayerId           = PlayerId.From(e.PlayerId);
                Name               = e.Name;
                LastResourceUpdate = e.OccurredAt;
                Resources          = e.StarterResources;
                _buildings         = Enum.GetValues<BuildingType>()
                                         .ToDictionary(b => b, _ => 0);
                break;

            case ResourcesCollectedEvent e:
                Resources          = Resources.Add(e.Produced);
                LastResourceUpdate = e.CollectedAt;
                break;

            case BuildingUpgradeStartedEvent e:
                Resources     = Resources.Subtract(e.Cost);
                ActiveUpgrade = new BuildingUpgrade(e.BuildingType, e.TargetLevel, e.CompletesAt);
                break;

            case BuildingUpgradeCompletedEvent e:
                _buildings[e.BuildingType] = e.NewLevel;
                ActiveUpgrade              = null;
                break;
        }
    }
}
