using CorsairTide.Server.Domain.Islands.Events;
using CorsairTide.Server.Domain.Players.Events;

namespace CorsairTide.Server.Infrastructure.EventStore;

/// <summary>
/// Maps event type names (stored as strings in JSON) back to their CLR types.
/// Add a new entry here whenever you introduce a new domain event.
/// </summary>
public static class EventTypeRegistry
{
    private static readonly Dictionary<string, Type> Registry = new()
    {
        // Island events
        [nameof(IslandCreatedEvent)]            = typeof(IslandCreatedEvent),
        [nameof(ResourcesCollectedEvent)]       = typeof(ResourcesCollectedEvent),
        [nameof(BuildingUpgradeStartedEvent)]   = typeof(BuildingUpgradeStartedEvent),
        [nameof(BuildingUpgradeCompletedEvent)] = typeof(BuildingUpgradeCompletedEvent),
        // Player events
        [nameof(PlayerRegisteredEvent)]         = typeof(PlayerRegisteredEvent),
    };

    public static Type? Resolve(string eventType) =>
        Registry.GetValueOrDefault(eventType);
}
