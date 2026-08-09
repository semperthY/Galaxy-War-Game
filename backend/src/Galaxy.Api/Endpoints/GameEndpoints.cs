using Galaxy.Application.Economy;
using Galaxy.Application.Games;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game");

        group.MapPost("/new", CreateGameAsync);
        group.MapGet("/current", GetCurrentGameAsync);
        group.MapPost("/resources/collect", CollectResourcesAsync);
    }

    private static async Task<IResult> CreateGameAsync(
        CreateGameRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Players.AnyAsync(cancellationToken))
        {
            return Results.Conflict(new
            {
                error = "A local game already exists."
            });
        }

        NewGame game;

        try
        {
            game = NewGameFactory.Create(request.Username, request.Race);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }

        dbContext.Players.Add(game.Player);
        dbContext.StarSystems.AddRange(game.StarSystems);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            "/api/game/current",
            CreateResponse(
                game.Player,
                game.Homeworld.StarSystem,
                game.Homeworld));
    }

    private static async Task<IResult> GetCurrentGameAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await dbContext.Planets
            .AsNoTracking()
            .Include(x => x.Player)
            .Include(x => x.StarSystem)
            .Where(x => x.PlayerId != null)
            .OrderBy(x => x.StarSystem.SystemNumber)
            .ThenBy(x => x.Position)
            .FirstOrDefaultAsync(cancellationToken);

        return planet is null
            ? Results.NotFound()
            : Results.Ok(CreateResponse(
                planet.Player!,
                planet.StarSystem,
                planet));
    }

    private static async Task<IResult> CollectResourcesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await dbContext.Planets
            .Include(x => x.Player)
            .Include(x => x.StarSystem)
            .Where(x => x.PlayerId != null)
            .OrderBy(x => x.StarSystem.SystemNumber)
            .ThenBy(x => x.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (planet is null)
        {
            return Results.NotFound();
        }

        ResourceProductionCalculator.Update(
            planet,
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateResponse(
            planet.Player!,
            planet.StarSystem,
            planet));
    }

    private static GameResponse CreateResponse(
        Player player,
        StarSystem starSystem,
        Planet planet)
    {
        var energy = EnergyCalculator.Calculate(planet);
        var storage = StorageCalculator.Calculate(planet);

        return new GameResponse(
            player.Id,
            player.Username,
            player.Race,
            planet.Id,
            planet.Name,
            starSystem.GalaxyNumber,
            starSystem.SystemNumber,
            planet.Position,
            planet.Materials,
            planet.Deuterium,
            storage.Materials,
            storage.Deuterium,
            planet.MaterialsExtractorLevel,
            planet.DeuteriumExtractorLevel,
            planet.PowerPlantLevel,
            energy.Production,
            energy.Consumption,
            energy.Efficiency,
            planet.ResourcesUpdatedAt);
    }
}

public sealed record CreateGameRequest(string Username, RaceType Race);

public sealed record GameResponse(
    Guid PlayerId,
    string Username,
    RaceType Race,
    Guid PlanetId,
    string PlanetName,
    int Galaxy,
    int System,
    int Position,
    decimal Materials,
    decimal Deuterium,
    decimal MaterialsCapacity,
    decimal DeuteriumCapacity,
    int MaterialsExtractorLevel,
    int DeuteriumExtractorLevel,
    int PowerPlantLevel,
    decimal EnergyProduction,
    decimal EnergyConsumption,
    decimal ProductionEfficiency,
    DateTime ResourcesUpdatedAt);



