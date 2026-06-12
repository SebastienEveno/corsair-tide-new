using CorsairTide.Server.Application.Islands;
using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.API;

public record StartUpgradeRequest(string BuildingType);

public static class IslandEndpoints
{
    public static IEndpointRouteBuilder MapIslandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/islands")
            .WithTags("Islands");

        // GET /api/islands/{playerId}
        // Returns the island with current resources (lazy-calculated) for a player.
        group.MapGet("/{playerId:guid}", async (
            Guid playerId,
            IslandService svc,
            CancellationToken ct) =>
        {
            var island = await svc.GetIslandAsync(playerId, ct);
            return island is null
                ? Results.NotFound(new { error = "No island found for this player." })
                : Results.Ok(island);
        })
        .WithName("GetIsland")
        .WithSummary("Get the island and current resources for a player.");

        // POST /api/islands/{playerId}/upgrades
        // Body: { "buildingType": "Sawmill" }
        group.MapPost("/{playerId:guid}/upgrades", async (
            Guid playerId,
            StartUpgradeRequest request,
            IslandService svc,
            CancellationToken ct) =>
        {
            try
            {
                var island = await svc.StartUpgradeAsync(playerId, request.BuildingType, ct);
                return Results.Ok(island);
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("StartBuildingUpgrade")
        .WithSummary("Start upgrading a building. Body: { buildingType: string }");

        return app;
    }
}
