using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;

namespace CorsairTide.Server.Application.Players;

public record RegisterPlayerRequest(string Username, string Email);
public record PlayerDto(Guid PlayerId, string Username, string Email);

/// <summary>
/// Handles player registration.
/// Creates the Player aggregate and a starter Island in the same transaction
/// (both event streams are written before this method returns).
/// </summary>
public class PlayerService(
    IPlayerRepository playerRepository,
    IIslandRepository islandRepository)
{
    public async Task<PlayerDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var player = await playerRepository.GetByUsernameAsync(username, ct);
        if (player is null) return null;
        return new PlayerDto(player.Id.Value, player.Username, player.Email);
    }

    public async Task<PlayerDto> RegisterAsync(
        RegisterPlayerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new DomainException("Username cannot be empty.");

        if (await playerRepository.UsernameExistsAsync(request.Username, ct))
            throw new DomainException($"Username '{request.Username}' is already taken.");

        // Create player
        var player = Player.Register(request.Username.Trim(), request.Email.Trim());
        await playerRepository.SaveAsync(player, ct);

        // Create starter island
        var island = Island.Create(player.Id, $"{player.Username}'s Island");
        await islandRepository.SaveAsync(island, ct);

        return new PlayerDto(player.Id.Value, player.Username, player.Email);
    }
}
