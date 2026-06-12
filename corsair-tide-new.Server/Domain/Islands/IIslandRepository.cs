using CorsairTide.Server.Domain.Players;

namespace CorsairTide.Server.Domain.Islands;

public interface IIslandRepository
{
    Task<Island?> GetByIdAsync(IslandId id, CancellationToken ct = default);
    Task<Island?> GetByPlayerIdAsync(PlayerId playerId, CancellationToken ct = default);
    Task SaveAsync(Island island, CancellationToken ct = default);
}
