using CorsairTide.Server.Application.Islands;
using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Islands.Events;
using CorsairTide.Server.Domain.Players;
using Moq;
using Xunit;

namespace CorsairTide.Server.Tests.Application.Islands;

public class IslandServiceTests
{
    private readonly Mock<IIslandRepository> _islandRepo;
    private readonly IslandService _sut;

    public IslandServiceTests()
    {
        _islandRepo = new Mock<IIslandRepository>();
        _sut = new IslandService(_islandRepo.Object);
    }

    // ── GetIslandAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetIslandAsync_WhenNoIslandExists_ShouldReturnNull()
    {
        // Arrange
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Island?)null);

        // Act
        var result = await _sut.GetIslandAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIslandAsync_WhenIslandExists_ShouldReturnDto()
    {
        // Arrange
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        var result = await _sut.GetIslandAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(island.Name, result.Name);
        Assert.Equal(island.Id.Value, result.IslandId);
    }

    [Fact]
    public async Task GetIslandAsync_WhenIslandExists_ShouldSaveAfterBringingUpToDate()
    {
        // Arrange
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        await _sut.GetIslandAsync(Guid.NewGuid());

        // Assert
        _islandRepo.Verify(
            r => r.SaveAsync(It.IsAny<Island>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetIslandAsync_ShouldReturnProductionRatesForCurrentBuildingLevels()
    {
        // Arrange — island with Sawmill at level 0 (no production)
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        var result = await _sut.GetIslandAsync(Guid.NewGuid());

        // Assert — Sawmill is level 0, so production rate is 0
        Assert.Equal(0, result!.WoodPerHour);
    }

    // ── StartUpgradeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task StartUpgradeAsync_WithUnknownBuildingType_ShouldThrowDomainException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => _sut.StartUpgradeAsync(Guid.NewGuid(), "GhostShip"));
    }

    [Fact]
    public async Task StartUpgradeAsync_WhenIslandNotFound_ShouldThrowDomainException()
    {
        // Arrange
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Island?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => _sut.StartUpgradeAsync(Guid.NewGuid(), "Sawmill"));
    }

    [Fact]
    public async Task StartUpgradeAsync_WithValidRequest_ShouldReturnDtoWithActiveUpgrade()
    {
        // Arrange
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        var result = await _sut.StartUpgradeAsync(Guid.NewGuid(), "Sawmill");

        // Assert
        Assert.NotNull(result.ActiveUpgrade);
        Assert.Equal("Sawmill", result.ActiveUpgrade.BuildingType);
        Assert.Equal(1, result.ActiveUpgrade.TargetLevel);
        Assert.True(result.ActiveUpgrade.SecondsRemaining > 0);
    }

    [Fact]
    public async Task StartUpgradeAsync_WithValidRequest_ShouldDeductCostInDto()
    {
        // Arrange
        var island = CreateTestIsland();
        var woodBefore = island.Resources.Wood;
        var cost = BuildingCosts.UpgradeCost(BuildingType.Sawmill, 1);

        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        var result = await _sut.StartUpgradeAsync(Guid.NewGuid(), "Sawmill");

        // Assert
        Assert.Equal(woodBefore - cost.Wood, result.Resources.Wood);
    }

    [Fact]
    public async Task StartUpgradeAsync_WithValidRequest_ShouldSaveIsland()
    {
        // Arrange
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        await _sut.StartUpgradeAsync(Guid.NewGuid(), "Sawmill");

        // Assert
        _islandRepo.Verify(
            r => r.SaveAsync(island, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartUpgradeAsync_IsCaseInsensitiveForBuildingType()
    {
        // Arrange
        var island = CreateTestIsland();
        _islandRepo
            .Setup(r => r.GetByPlayerIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(island);

        // Act
        var result = await _sut.StartUpgradeAsync(Guid.NewGuid(), "sawmill");

        // Assert
        Assert.NotNull(result.ActiveUpgrade);
        Assert.Equal("Sawmill", result.ActiveUpgrade.BuildingType);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static Island CreateTestIsland(Guid? playerId = null)
    {
        return Island.Reconstitute([
            new IslandCreatedEvent(
                Guid.NewGuid(),
                playerId ?? Guid.NewGuid(),
                "Test Island",
                new Resources(500, 300, 100, 50))
        ]);
    }
}
