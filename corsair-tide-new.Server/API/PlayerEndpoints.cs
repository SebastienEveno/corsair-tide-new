using CorsairTide.Server.Application.Players;
using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.API;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players")
            .WithTags("Players");

        // GET /api/players/{username}
        group.MapGet("/{username}", async (
            string username,
            PlayerService svc,
            CancellationToken ct) =>
        {
            var player = await svc.GetByUsernameAsync(username, ct);
            return player is null ? Results.NotFound() : Results.Ok(player);
        })
        .WithName("GetPlayer")
        .WithSummary("Look up a player by username.");

        // POST /api/players/register
        // Body: { "username": "...", "email": "..." }
        group.MapPost("/register", async (
            RegisterPlayerRequest request,
            PlayerService svc,
            CancellationToken ct) =>
        {
            try
            {
                var player = await svc.RegisterAsync(request, ct);
                return Results.Created($"/api/players/{player.PlayerId}", player);
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RegisterPlayer")
        .WithSummary("Register a new player and create their starter island.");

        return app;
    }
}
