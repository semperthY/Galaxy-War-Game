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
            game = NewGameFactory.Create(request.Username);
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
        var game = await dbContext.Planets
            .AsNoTracking()
            .Where(planet => planet.PlayerId != null)
            .Select(planet => new GameResponse(
                planet.Player!.Id,
                planet.Player.Username,
                planet.Id,
                planet.Name,
                planet.StarSystem.GalaxyNumber,
                planet.StarSystem.SystemNumber,
                planet.Position,
                planet.Metal,
                planet.Crystal,
                planet.Deuterium,
                planet.MetalMineLevel,
                planet.CrystalMineLevel,
                planet.DeuteriumMineLevel,
                planet.ResourcesUpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return game is null
            ? Results.NotFound()
            : Results.Ok(game);
    }

    private static async Task<IResult> CollectResourcesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await dbContext.Planets
            .Include(x => x.Player)
            .Include(x => x.StarSystem)
            .SingleOrDefaultAsync(
                x => x.PlayerId != null,
                cancellationToken);

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
        return new GameResponse(
            player.Id,
            player.Username,
            planet.Id,
            planet.Name,
            starSystem.GalaxyNumber,
            starSystem.SystemNumber,
            planet.Position,
            planet.Metal,
            planet.Crystal,
            planet.Deuterium,
            planet.MetalMineLevel,
            planet.CrystalMineLevel,
            planet.DeuteriumMineLevel,
            planet.ResourcesUpdatedAt);
    }
}

public sealed record CreateGameRequest(string Username);

public sealed record GameResponse(
    Guid PlayerId,
    string Username,
    Guid PlanetId,
    string PlanetName,
    int Galaxy,
    int System,
    int Position,
    decimal Metal,
    decimal Crystal,
    decimal Deuterium,
    int MetalMineLevel,
    int CrystalMineLevel,
    int DeuteriumMineLevel,
    DateTime ResourcesUpdatedAt);
