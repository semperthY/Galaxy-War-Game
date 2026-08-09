using Galaxy.Api.Endpoints;
using Galaxy.Api.Hosting;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseTestAccessProtection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>(
        "DevelopmentTools:Enabled"))
{
    app.MapDevelopmentEndpoints();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGameEndpoints();
app.MapPlanetEndpoints();
app.MapGalaxyEndpoints();
app.MapBuildingEndpoints();
app.MapResearchEndpoints();
app.MapProductionEndpoints();
app.MapComponentCatalogEndpoints();
app.MapShipBlueprintEndpoints();
app.MapShipAssemblyEndpoints();
app.MapColonizationEndpoints();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy"
}));

app.Run();
