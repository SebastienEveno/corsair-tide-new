using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;

namespace CorsairTide.Server.Domain.Islands.Events;

/// <summary>Raised when a new island is created for a player.</summary>
public record IslandCreatedEvent(
    Guid IslandId,
    Guid PlayerId,
    string Name,
    Resources StarterResources) : DomainEvent;

/// <summary>
/// Raised on every read to record that passive-income resources
/// accumulated since the last update.
/// </summary>
public record ResourcesCollectedEvent(
    Guid IslandId,
    Resources Produced,
    DateTimeOffset CollectedAt) : DomainEvent;

/// <summary>Raised when the player queues a building upgrade.</summary>
public record BuildingUpgradeStartedEvent(
    Guid IslandId,
    BuildingType BuildingType,
    int FromLevel,
    int TargetLevel,
    DateTimeOffset StartsAt,
    DateTimeOffset CompletesAt,
    Resources Cost) : DomainEvent;

/// <summary>Raised (lazily) when a queued upgrade's completion time has passed.</summary>
public record BuildingUpgradeCompletedEvent(
    Guid IslandId,
    BuildingType BuildingType,
    int NewLevel,
    DateTimeOffset CompletedAt) : DomainEvent;
