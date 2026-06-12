using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.Domain.Players.Events;

public record PlayerRegisteredEvent(
    Guid PlayerId,
    string Username,
    string Email,
    string PasswordHash) : DomainEvent;
