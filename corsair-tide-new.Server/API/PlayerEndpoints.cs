using CorsairTide.Server.Application.Players;
using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.API;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players")
            .WithTags("Players");

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
