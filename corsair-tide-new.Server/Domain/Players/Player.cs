using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Players.Events;

namespace CorsairTide.Server.Domain.Players;

public class Player : AggregateRoot<PlayerId>
{
    private Player() { }

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    // ── Factory: new player ──────────────────────────────────────────────

    public static Player Register(string username, string email, string passwordHash)
    {
        var player = new Player();
        player.Raise(new PlayerRegisteredEvent(Guid.NewGuid(), username, email, passwordHash));
        return player;
    }

    // ── Factory: reconstitute from event history ─────────────────────────

    public static Player Reconstitute(IEnumerable<DomainEvent> history)
    {
        var player = new Player();
        player.Load(history);
        return player;
    }

    // ── Event application ────────────────────────────────────────────────

    protected override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case PlayerRegisteredEvent e:
                Id = PlayerId.From(e.PlayerId);
                Username = e.Username;
                Email = e.Email;
                PasswordHash = e.PasswordHash;
                break;
        }
    }
}
