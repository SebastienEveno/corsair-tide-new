using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CorsairTide.Server.Application.Islands;
using CorsairTide.Server.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CorsairTide.Server.API;

public record StartUpgradeRequest(string BuildingType);

public static class IslandEndpoints
{
    public static IEndpointRouteBuilder MapIslandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/islands")
            .WithTags("Islands")
            .RequireAuthorization();

        // GET /api/islands/me
        // Returns the island for the authenticated player.
        group.MapGet("/me", async (
            ClaimsPrincipal user,
            IslandService svc,
            CancellationToken ct) =>
        {
            var playerId = GetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            var island = await svc.GetIslandAsync(playerId.Value, ct);
            return island is null
                ? Results.NotFound(new { error = "No island found for this player." })
                : Results.Ok(island);
        })
        .WithName("GetMyIsland")
        .WithSummary("Get the island and current resources for the authenticated player.");

        // POST /api/islands/me/upgrades
        // Body: { "buildingType": "Sawmill" }
        group.MapPost("/me/upgrades", async (
            ClaimsPrincipal user,
            [FromBody] StartUpgradeRequest request,
            IslandService svc,
            CancellationToken ct) =>
        {
            var playerId = GetPlayerId(user);
            if (playerId is null) return Results.Unauthorized();

            try
            {
                var island = await svc.StartUpgradeAsync(playerId.Value, request.BuildingType, ct);
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

    private static Guid? GetPlayerId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
