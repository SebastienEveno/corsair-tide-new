namespace CorsairTide.Server.Domain.Common;

/// <summary>
/// Base type for all domain events. Every event is immutable and carries
/// a unique ID and the timestamp it occurred.
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
