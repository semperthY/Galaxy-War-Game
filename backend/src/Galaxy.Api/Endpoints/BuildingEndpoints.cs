using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class BuildingEndpoints
{
    public static void MapBuildingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game/buildings");

        group.MapGet("/", GetStatusAsync);
        group.MapPost("/{building}/start", StartAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await GetPlanetAsync(
            dbContext,
            cancellationToken);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var utcNow = DateTime.UtcNow;

        BuildingService.Complete(planet, utcNow);
        ResourceProductionCalculator.Update(planet, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(planet));
    }

    private static async Task<IResult> StartAsync(
        BuildingType building,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await GetPlanetAsync(
            dbContext,
            cancellationToken);

        if (planet is null)
        {
            return Results.NotFound();
        }

        var utcNow = DateTime.UtcNow;

        BuildingService.Complete(planet, utcNow);

        try
        {
            BuildingService.Start(
                planet,
                building,
                utcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(planet));
    }

    private static Task<Planet?> GetPlanetAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.Planets.SingleOrDefaultAsync(
            x => x.PlayerId != null,
            cancellationToken);
    }

    private static BuildingStatusResponse CreateStatus(
        Planet planet)
    {
        var energy = EnergyCalculator.Calculate(planet);
        var storage = StorageCalculator.Calculate(planet);

        var buildings = Enum.GetValues<BuildingType>()
            .Select(building =>
            {
                var currentLevel = BuildingService.GetLevel(
                    planet,
                    building);

                return new BuildingOptionResponse(
                    building,
                    currentLevel,
                    BuildingService.CalculateCost(
                        building,
                        currentLevel + 1));
            })
            .ToList();

        return new BuildingStatusResponse(
            planet.Materials,
            planet.Deuterium,
            storage.Materials,
            storage.Deuterium,
            energy.Production,
            energy.Consumption,
            energy.Efficiency,
            BuildingService.GetUsedSites(planet),
            planet.BuildingSiteCapacity,
            planet.QueuedBuilding,
            planet.QueuedBuildingLevel,
            planet.BuildingCompletesAt,
            buildings);
    }
}

public sealed record BuildingStatusResponse(
    decimal Materials,
    decimal Deuterium,
    decimal MaterialsCapacity,
    decimal DeuteriumCapacity,
    decimal EnergyProduction,
    decimal EnergyConsumption,
    decimal ProductionEfficiency,
    int UsedBuildingSites,
    int BuildingSiteCapacity,
    BuildingType? QueuedBuilding,
    int? QueuedBuildingLevel,
    DateTime? BuildingCompletesAt,
    List<BuildingOptionResponse> Buildings);

public sealed record BuildingOptionResponse(
    BuildingType Building,
    int CurrentLevel,
    BuildingCost NextLevelCost);

