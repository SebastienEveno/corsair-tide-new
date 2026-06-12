namespace CorsairTide.Server.Infrastructure.EventStore;

/// <summary>
/// On-disk envelope for a single domain event.
/// <see cref="Payload"/> is the raw JSON of the concrete event type,
/// and <see cref="EventType"/> is the class name used to look up the
/// CLR type in <see cref="EventTypeRegistry"/> during deserialization.
/// </summary>
public record StoredEvent(
    string EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAt,
    string Payload);
