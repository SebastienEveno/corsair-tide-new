using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Islands.Events;
using CorsairTide.Server.Domain.Players;
using Xunit;

namespace CorsairTide.Server.Tests.Domain.Islands;

public class IslandAggregateTests
{
    private static readonly PlayerId TestPlayerId = PlayerId.New();
    private static readonly Resources StarterResources = new(500, 300, 100, 50);

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldRaiseIslandCreatedEvent()
    {
        // Arrange & Act
        var island = Island.Create(TestPlayerId, "Black Pearl");

        // Assert
        Assert.Single(island.UncommittedEvents);
        Assert.IsType<IslandCreatedEvent>(island.UncommittedEvents[0]);
    }

    [Fact]
    public void Create_ShouldSetPlayerIdAndName()
    {
        // Arrange & Act
        var island = Island.Create(TestPlayerId, "Black Pearl");

        // Assert
        Assert.Equal(TestPlayerId, island.PlayerId);
        Assert.Equal("Black Pearl", island.Name);
    }

    [Fact]
    public void Create_ShouldSeedStarterResources()
    {
        // Arrange & Act
        var island = Island.Create(TestPlayerId, "Black Pearl");

        // Assert
        Assert.True(island.Resources.Wood > 0);
        Assert.True(island.Resources.Gold > 0);
    }

    [Fact]
    public void Create_ShouldInitialiseAllBuildingsAtLevelZero()
    {
        // Arrange & Act
        var island = Island.Create(TestPlayerId, "Black Pearl");

        // Assert
        foreach (var type in Enum.GetValues<BuildingType>())
            Assert.Equal(0, island.Buildings[type]);
    }

    [Fact]
    public void Create_ShouldAssignNonEmptyIslandId()
    {
        // Arrange & Act
        var island = Island.Create(TestPlayerId, "Black Pearl");

        // Assert
        Assert.NotEqual(Guid.Empty, island.Id.Value);
    }

    // ── StartUpgrade ─────────────────────────────────────────────────────

    [Fact]
    public void StartUpgrade_WithSufficientResources_ShouldRaiseUpgradeStartedEvent()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var now = island.LastResourceUpdate;

        // Act
        island.StartUpgrade(BuildingType.Sawmill, now);

        // Assert
        var evt = island.UncommittedEvents.OfType<BuildingUpgradeStartedEvent>().Single();
        Assert.Equal(BuildingType.Sawmill, evt.BuildingType);
        Assert.Equal(0, evt.FromLevel);
        Assert.Equal(1, evt.TargetLevel);
    }

    [Fact]
    public void StartUpgrade_ShouldDeductResourceCostFromIsland()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var woodBefore = island.Resources.Wood;
        var cost = BuildingCosts.UpgradeCost(BuildingType.Sawmill, 1);
        var now = island.LastResourceUpdate;

        // Act
        island.StartUpgrade(BuildingType.Sawmill, now);

        // Assert
        Assert.Equal(woodBefore - cost.Wood, island.Resources.Wood);
    }

    [Fact]
    public void StartUpgrade_ShouldSetActiveUpgrade()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var now = island.LastResourceUpdate;

        // Act
        island.StartUpgrade(BuildingType.Sawmill, now);

        // Assert
        Assert.NotNull(island.ActiveUpgrade);
        Assert.Equal(BuildingType.Sawmill, island.ActiveUpgrade.Type);
        Assert.Equal(1, island.ActiveUpgrade.TargetLevel);
    }

    [Fact]
    public void StartUpgrade_WithInsufficientResources_ShouldThrowDomainException()
    {
        // Arrange — reconstitute an island that was created with zero resources
        var island = Island.Reconstitute([
            new IslandCreatedEvent(Guid.NewGuid(), TestPlayerId.Value, "Poor Island", Resources.Zero)
        ]);

        // Act & Assert
        Assert.Throws<DomainException>(
            () => island.StartUpgrade(BuildingType.Sawmill, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void StartUpgrade_WhenUpgradeAlreadyInProgress_ShouldThrowDomainException()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var now = island.LastResourceUpdate;
        island.StartUpgrade(BuildingType.Sawmill, now);

        // Act & Assert
        Assert.Throws<DomainException>(
            () => island.StartUpgrade(BuildingType.GoldMine, now));
    }

    [Fact]
    public void StartUpgrade_WhenPreviousUpgradeJustCompleted_ShouldAllowNewUpgrade()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var now = island.LastResourceUpdate;
        island.StartUpgrade(BuildingType.Sawmill, now);
        var afterCompletion = island.ActiveUpgrade!.CompletesAt.AddSeconds(1);

        // Act — upgrade should be auto-completed by BringUpToDate / StartUpgrade
        island.StartUpgrade(BuildingType.GoldMine, afterCompletion);

        // Assert
        Assert.NotNull(island.ActiveUpgrade);
        Assert.Equal(BuildingType.GoldMine, island.ActiveUpgrade.Type);
    }

    // ── BringUpToDate ────────────────────────────────────────────────────

    [Fact]
    public void BringUpToDate_WithSawmillAtLevelOne_ShouldAccumulateWood()
    {
        // Arrange
        var island = IslandWithBuilding(BuildingType.Sawmill, level: 1);
        var oneHourLater = island.LastResourceUpdate.AddHours(1);

        // Act
        island.BringUpToDate(oneHourLater);

        // Assert
        var evt = island.UncommittedEvents.OfType<ResourcesCollectedEvent>().Single();
        Assert.True(evt.Produced.Wood > 0);
    }

    [Fact]
    public void BringUpToDate_WithNoProductionBuildings_ShouldNotRaiseResourcesCollectedEvent()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        island.ClearUncommittedEvents();
        var oneHourLater = island.LastResourceUpdate.AddHours(1);

        // Act
        island.BringUpToDate(oneHourLater);

        // Assert
        Assert.Empty(island.UncommittedEvents.OfType<ResourcesCollectedEvent>());
    }

    [Fact]
    public void BringUpToDate_WithLessThanOneSecondElapsed_ShouldNotCollectResources()
    {
        // Arrange
        var island = IslandWithBuilding(BuildingType.Sawmill, level: 1);
        var halfSecondLater = island.LastResourceUpdate.AddMilliseconds(500);

        // Act
        island.BringUpToDate(halfSecondLater);

        // Assert
        Assert.Empty(island.UncommittedEvents.OfType<ResourcesCollectedEvent>());
    }

    [Fact]
    public void BringUpToDate_WhenUpgradeCompleted_ShouldCompleteUpgradeBeforeCollectingResources()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl");
        var now = island.LastResourceUpdate;
        island.StartUpgrade(BuildingType.Sawmill, now);
        island.ClearUncommittedEvents();

        var afterCompletion = island.ActiveUpgrade!.CompletesAt.AddSeconds(10);

        // Act
        island.BringUpToDate(afterCompletion);

        // Assert
        Assert.Contains(island.UncommittedEvents, e => e is BuildingUpgradeCompletedEvent);
        Assert.Equal(1, island.Buildings[BuildingType.Sawmill]);
        // Resources collected at the new (upgraded) production rate
        Assert.Contains(island.UncommittedEvents, e => e is ResourcesCollectedEvent);
    }

    [Fact]
    public void BringUpToDate_ShouldCapResourcesAtStorageCapacity()
    {
        // Arrange — island with almost-full wood storage
        var nearCapacityResources = new Resources(Wood: 999, Gold: 0, Rum: 0, Food: 0);
        var island = Island.Reconstitute([
            new IslandCreatedEvent(Guid.NewGuid(), TestPlayerId.Value, "Black Pearl", nearCapacityResources),
            new BuildingUpgradeCompletedEvent(Guid.NewGuid(), BuildingType.Sawmill, 5, DateTimeOffset.UtcNow.AddHours(-2))
        ]);
        var farFuture = island.LastResourceUpdate.AddDays(30);

        // Act
        island.BringUpToDate(farFuture);

        // Assert — wood must not exceed storage cap (1000 at WoodStorage level 0)
        var capacity = StorageCapacities.Get(island.Buildings);
        Assert.True(island.Resources.Wood <= capacity.Wood);
    }

    // ── Reconstitute ─────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_ShouldRebuildStateFromEvents()
    {
        // Arrange
        var original = Island.Create(TestPlayerId, "Black Pearl");
        var now = original.LastResourceUpdate;
        original.StartUpgrade(BuildingType.Sawmill, now);
        var events = original.UncommittedEvents.ToList();

        // Act
        var reconstituted = Island.Reconstitute(events);

        // Assert
        Assert.Equal(original.Id, reconstituted.Id);
        Assert.Equal(original.PlayerId, reconstituted.PlayerId);
        Assert.Equal(original.Name, reconstituted.Name);
        Assert.NotNull(reconstituted.ActiveUpgrade);
        Assert.Equal(BuildingType.Sawmill, reconstituted.ActiveUpgrade.Type);
    }

    [Fact]
    public void Reconstitute_ShouldHaveNoUncommittedEvents()
    {
        // Arrange
        var events = Island.Create(TestPlayerId, "Black Pearl").UncommittedEvents.ToList();

        // Act
        var reconstituted = Island.Reconstitute(events);

        // Assert
        Assert.Empty(reconstituted.UncommittedEvents);
    }

    [Fact]
    public void Reconstitute_AfterUpgradeCompleted_ShouldReflectNewBuildingLevel()
    {
        // Arrange
        var islandId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow.AddHours(-1);

        var events = new List<DomainEvent>
        {
            new IslandCreatedEvent(islandId, TestPlayerId.Value, "Black Pearl", StarterResources),
            new BuildingUpgradeStartedEvent(
                islandId, BuildingType.Sawmill, 0, 1,
                completedAt.AddSeconds(-30), completedAt,
                BuildingCosts.UpgradeCost(BuildingType.Sawmill, 1)),
            new BuildingUpgradeCompletedEvent(islandId, BuildingType.Sawmill, 1, completedAt)
        };

        // Act
        var island = Island.Reconstitute(events);

        // Assert
        Assert.Equal(1, island.Buildings[BuildingType.Sawmill]);
        Assert.Null(island.ActiveUpgrade);
    }

    // ── Version tracking ─────────────────────────────────────────────────

    [Fact]
    public void Version_ShouldIncrementWithEachRaisedEvent()
    {
        // Arrange
        var island = Island.Create(TestPlayerId, "Black Pearl"); // version 1
        var now = island.LastResourceUpdate;

        // Act
        island.StartUpgrade(BuildingType.Sawmill, now); // version 2

        // Assert
        Assert.Equal(2, island.Version);
    }

    // ── Test helpers ─────────────────────────────────────────────────────

    /// <summary>Creates a reconstituted island with a specific building at the given level.</summary>
    private static Island IslandWithBuilding(BuildingType type, int level)
    {
        var islandId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);

        var events = new List<DomainEvent>
        {
            new IslandCreatedEvent(islandId, TestPlayerId.Value, "Black Pearl", StarterResources)
                { OccurredAt = createdAt }
        };

        var current = createdAt;
        for (var i = 1; i <= level; i++)
        {
            var duration = BuildingCosts.UpgradeDuration(type, i);
            var cost = BuildingCosts.UpgradeCost(type, i);
            events.Add(new BuildingUpgradeStartedEvent(islandId, type, i - 1, i, current, current + duration, cost));
            events.Add(new BuildingUpgradeCompletedEvent(islandId, type, i, current + duration));
            current += duration;
        }

        return Island.Reconstitute(events);
    }
}
