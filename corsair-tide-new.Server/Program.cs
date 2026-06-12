using System.Text.Json.Serialization;
using CorsairTide.Server.API;
using CorsairTide.Server.Application.Islands;
using CorsairTide.Server.Application.Players;
using CorsairTide.Server.Domain.Islands;
using CorsairTide.Server.Domain.Players;
using CorsairTide.Server.Infrastructure.EventStore;
using CorsairTide.Server.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire defaults (service discovery, resilience, OpenTelemetry) ────────────
builder.AddServiceDefaults();

// ── Core ASP.NET services ─────────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Serialize enums as strings (e.g. "Sawmill" instead of 0) in all API responses
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Event store (JSON files for local dev) ────────────────────────────────────
// Events are written to  {ContentRoot}/event-store/
// Add event-store/ to your .gitignore — it is local state, not source code.
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

// ── API endpoints ─────────────────────────────────────────────────────────────
app.MapIslandEndpoints();
app.MapPlayerEndpoints();

app.MapDefaultEndpoints();
app.UseFileServer();

app.Run();
