using System.Text;
using System.Text.Json.Serialization;
using CorsairTide.Server.API;
using CorsairTide.Server.Application.Auth;
using CorsairTide.Server.Application.Islands;
using CorsairTide.Server.Application.Players;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;
using CorsairTide.Server.Infrastructure.EventStore;
using CorsairTide.Server.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire defaults (service discovery, resilience, OpenTelemetry) ────────────
builder.AddServiceDefaults();

// ── Core ASP.NET services ─────────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Serialize enums as strings (e.g. "Sawmill" instead of 0) in all API responses
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── JWT authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

// ── Event store (JSON files for local dev) ────────────────────────────────────
var eventStorePath = Path.Combine(builder.Environment.ContentRootPath, "event-store");
builder.Services.AddSingleton<IEventStore>(_ => new JsonFileEventStore(eventStorePath));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IIslandRepository>(sp =>
    new IslandRepository(sp.GetRequiredService<IEventStore>(), eventStorePath));

builder.Services.AddScoped<IPlayerRepository>(sp =>
    new PlayerRepository(sp.GetRequiredService<IEventStore>(), eventStorePath));

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IslandService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<AuthService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Corsair Tide API");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

// ── API endpoints ─────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapIslandEndpoints();
app.MapPlayerEndpoints();

app.MapDefaultEndpoints();
app.UseFileServer();

app.Run();
