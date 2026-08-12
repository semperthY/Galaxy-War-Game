using Galaxy.Application.Economy;
using Galaxy.Api.Security;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class PlanetEndpoints
{
    public static void MapPlanetEndpoints(
        this WebApplication app)
    {
        app.MapGet("/api/game/planets", GetAllAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAllAsync(
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(
            httpContext.User,
            dbContext,
            cancellationToken);
        if (playerId is null)
        {
            return Results.NotFound();
        }

        var planets = await dbContext.Planets
            .Include(x => x.StarSystem)
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.StarSystem.GalaxyNumber)
            .ThenBy(x => x.StarSystem.SystemNumber)
            .ThenBy(x => x.Position)
            .ToListAsync(cancellationToken);

        if (planets.Count == 0)
        {
            return Results.NotFound();
        }

        var utcNow = DateTime.UtcNow;

        foreach (var planet in planets)
        {
            BuildingService.Complete(planet, utcNow);
            ResourceProductionCalculator.Update(planet, utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = planets
            .Select(planet =>
            {
                var energy = EnergyCalculator.Calculate(planet);
                var storage = StorageCalculator.Calculate(planet);

                return new PlanetSummaryResponse(
                    planet.Id,
                    planet.Name,
                    planet.StarSystem.GalaxyNumber,
                    planet.StarSystem.SystemNumber,
                    planet.Position,
                    planet.Materials,
                    planet.Deuterium,
                    storage.Materials,
                    storage.Deuterium,
                    energy.Production,
                    energy.Consumption,
                    energy.Efficiency,
                    BuildingService.GetUsedSites(planet),
                    planet.BuildingSiteCapacity);
            })
            .ToList();

        return Results.Ok(response);
    }
}

public sealed record PlanetSummaryResponse(
    Guid Id,
    string Name,
    int Galaxy,
    int System,
    int Position,
    decimal Materials,
    decimal Deuterium,
    decimal MaterialsCapacity,
    decimal DeuteriumCapacity,
    decimal EnergyProduction,
    decimal EnergyConsumption,
    decimal ProductionEfficiency,
    int UsedBuildingSites,
    int BuildingSiteCapacity);
