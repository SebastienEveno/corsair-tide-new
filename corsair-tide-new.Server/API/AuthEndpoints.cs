using CorsairTide.Server.Application.Auth;
using CorsairTide.Server.Domain.Common;

namespace CorsairTide.Server.API;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // POST /api/auth/login
        group.MapPost("/login", async (
            LoginRequest request,
            AuthService svc,
            CancellationToken ct) =>
        {
            try
            {
                var response = await svc.LoginAsync(request, ct);
                return Results.Ok(response);
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("Login")
        .WithSummary("Authenticate with username and password, returns a JWT.")
        .AllowAnonymous();

        return app;
    }
}
