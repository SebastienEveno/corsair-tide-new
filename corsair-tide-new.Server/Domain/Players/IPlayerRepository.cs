namespace CorsairTide.Server.Domain.Players;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(PlayerId id, CancellationToken ct = default);
    Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task SaveAsync(Player player, CancellationToken ct = default);
}
