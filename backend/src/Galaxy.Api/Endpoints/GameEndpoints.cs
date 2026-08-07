using Galaxy.Application.Games;
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
        dbContext.StarSystems.Add(game.StarSystem);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            "/api/game/current",
            CreateResponse(game.Player, game.StarSystem, game.Planet));
    }

    private static async Task<IResult> GetCurrentGameAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var game = await dbContext.Planets
            .AsNoTracking()
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
                planet.Deuterium))
            .SingleOrDefaultAsync(cancellationToken);

        return game is null
            ? Results.NotFound()
            : Results.Ok(game);
    }

    private static GameResponse CreateResponse(
        Galaxy.Domain.Entities.Player player,
        Galaxy.Domain.Entities.StarSystem starSystem,
        Galaxy.Domain.Entities.Planet planet)
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
            planet.Deuterium);
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
    long Metal,
    long Crystal,
    long Deuterium);
