using CorsairTide.Server.Application.Players;
using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;
using Moq;
using Xunit;

namespace CorsairTide.Server.Tests.Application.Players;

public class PlayerServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepo;
    private readonly Mock<IIslandRepository> _islandRepo;
    private readonly PlayerService _sut;

    public PlayerServiceTests()
    {
        _playerRepo = new Mock<IPlayerRepository>();
        _islandRepo = new Mock<IIslandRepository>();
        _sut = new PlayerService(_playerRepo.Object, _islandRepo.Object);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithAvailableUsername_ShouldReturnPlayerDto()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync("BlackBart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.Equal("BlackBart", result.Username);
        Assert.Equal("bart@sea.io", result.Email);
        Assert.NotEqual(Guid.Empty, result.PlayerId);
    }

    [Fact]
    public async Task RegisterAsync_WithTakenUsername_ShouldThrowDomainException()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync("BlackBart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyUsername_ShouldThrowDomainException()
    {
        // Arrange
        var request = new RegisterPlayerRequest("", "bart@sea.io");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithWhitespaceUsername_ShouldThrowDomainException()
    {
        // Arrange
        var request = new RegisterPlayerRequest("   ", "bart@sea.io");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_ShouldSavePlayerToRepository()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        _playerRepo.Verify(
            r => r.SaveAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAndSaveIslandForNewPlayer()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        _islandRepo.Verify(
            r => r.SaveAsync(It.IsAny<Island>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldNameIslandAfterPlayer()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Island? savedIsland = null;
        _islandRepo
            .Setup(r => r.SaveAsync(It.IsAny<Island>(), It.IsAny<CancellationToken>()))
            .Callback<Island, CancellationToken>((island, _) => savedIsland = island)
            .Returns(Task.CompletedTask);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(savedIsland);
        Assert.Contains("BlackBart", savedIsland.Name);
    }

    [Fact]
    public async Task RegisterAsync_ShouldLinkIslandToCorrectPlayer()
    {
        // Arrange
        _playerRepo
            .Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Player? savedPlayer = null;
        Island? savedIsland = null;

        _playerRepo
            .Setup(r => r.SaveAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()))
            .Callback<Player, CancellationToken>((p, _) => savedPlayer = p)
            .Returns(Task.CompletedTask);

        _islandRepo
            .Setup(r => r.SaveAsync(It.IsAny<Island>(), It.IsAny<CancellationToken>()))
            .Callback<Island, CancellationToken>((i, _) => savedIsland = i)
            .Returns(Task.CompletedTask);

        var request = new RegisterPlayerRequest("BlackBart", "bart@sea.io");

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(savedPlayer);
        Assert.NotNull(savedIsland);
        Assert.Equal(savedPlayer.Id, savedIsland.PlayerId);
    }
}
