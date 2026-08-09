using Galaxy.Application.Colonization;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ColonizationEndpoints
{
    public static void MapColonizationEndpoints(
        this WebApplication app)
    {
        app.MapPost(
            "/api/game/colonization/{targetPlanetId:guid}",
            ColonizeAsync);
    }

    private static async Task<IResult> ColonizeAsync(
        Guid targetPlanetId,
        ColonizePlanetRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Planets)
            .Include(x => x.Ships)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        var ship = await dbContext.Ships
            .Include(x => x.Planet)
            .Include(x => x.Blueprint)
            .ThenInclude(x => x.Modules)
            .SingleOrDefaultAsync(
                x =>
                    x.Id == request.ShipId &&
                    x.PlayerId == player.Id,
                cancellationToken);

        if (ship is null)
        {
            return Results.BadRequest(new
            {
                error = "Reserve ship does not exist."
            });
        }

        var targetPlanet = await dbContext.Planets
            .Include(x => x.StarSystem)
            .SingleOrDefaultAsync(
                x => x.Id == targetPlanetId,
                cancellationToken);

        if (targetPlanet is null)
        {
            return Results.NotFound();
        }

        ColonizationResult result;

        try
        {
            result = ColonizationService.Colonize(
                player,
                ship,
                targetPlanet,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }

        dbContext.Ships.Remove(result.ConsumedShip);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ColonizedPlanetResponse(
            result.Planet.Id,
            result.Planet.Name,
            result.Planet.StarSystem.GalaxyNumber,
            result.Planet.StarSystem.SystemNumber,
            result.Planet.Position,
            result.Planet.Materials,
            result.Planet.Deuterium,
            result.ConsumedShip.Id));
    }
}

public sealed record ColonizePlanetRequest(
    Guid ShipId);

public sealed record ColonizedPlanetResponse(
    Guid PlanetId,
    string PlanetName,
    int Galaxy,
    int System,
    int Position,
    decimal Materials,
    decimal Deuterium,
    Guid ConsumedShipId);
