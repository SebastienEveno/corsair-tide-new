using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Players;
using CorsairTide.Server.Domain.Players.Events;
using Xunit;

namespace CorsairTide.Server.Tests.Domain.Players;

public class PlayerAggregateTests
{
    // ── Register ─────────────────────────────────────────────────────────

    [Fact]
    public void Register_ShouldRaisePlayerRegisteredEvent()
    {
        // Arrange & Act
        var player = Player.Register("BlackBart", "bart@sea.io");

        // Assert
        Assert.Single(player.UncommittedEvents);
        Assert.IsType<PlayerRegisteredEvent>(player.UncommittedEvents[0]);
    }

    [Fact]
    public void Register_ShouldSetUsernameAndEmail()
    {
        // Arrange & Act
        var player = Player.Register("BlackBart", "bart@sea.io");

        // Assert
        Assert.Equal("BlackBart", player.Username);
        Assert.Equal("bart@sea.io", player.Email);
    }

    [Fact]
    public void Register_ShouldAssignNonEmptyPlayerId()
    {
        // Arrange & Act
        var player = Player.Register("BlackBart", "bart@sea.io");

        // Assert
        Assert.NotEqual(Guid.Empty, player.Id.Value);
    }

    [Fact]
    public void Register_TwoPlayers_ShouldHaveDifferentIds()
    {
        // Arrange & Act
        var player1 = Player.Register("BlackBart", "bart@sea.io");
        var player2 = Player.Register("Redbeard", "red@sea.io");

        // Assert
        Assert.NotEqual(player1.Id, player2.Id);
    }

    // ── Reconstitute ─────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_ShouldRebuildUsernameAndEmailFromEvents()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PlayerRegisteredEvent(Guid.NewGuid(), "BlackBart", "bart@sea.io")
        };

        // Act
        var player = Player.Reconstitute(events);

        // Assert
        Assert.Equal("BlackBart", player.Username);
        Assert.Equal("bart@sea.io", player.Email);
    }

    [Fact]
    public void Reconstitute_ShouldRestorePlayerId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var events = new List<DomainEvent>
        {
            new PlayerRegisteredEvent(id, "BlackBart", "bart@sea.io")
        };

        // Act
        var player = Player.Reconstitute(events);

        // Assert
        Assert.Equal(id, player.Id.Value);
    }

    [Fact]
    public void Reconstitute_ShouldHaveNoUncommittedEvents()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PlayerRegisteredEvent(Guid.NewGuid(), "BlackBart", "bart@sea.io")
        };

        // Act
        var player = Player.Reconstitute(events);

        // Assert
        Assert.Empty(player.UncommittedEvents);
    }

    // ── Version tracking ─────────────────────────────────────────────────

    [Fact]
    public void Version_ShouldBeOneAfterRegistration()
    {
        // Arrange & Act
        var player = Player.Register("BlackBart", "bart@sea.io");

        // Assert
        Assert.Equal(1, player.Version);
    }
}
