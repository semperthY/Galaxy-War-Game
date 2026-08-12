using Galaxy.Application.Colonization;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ColonizationEndpoints
{
    public static void MapColonizationEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/game/colonization");

        group.MapGet("/", GetStatusAsync);
        group.MapPost("/{targetPlanetId:guid}", BeginAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Planets)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        var operations = await dbContext.ColonizationOperations
            .Include(x => x.TargetPlanet)
            .ThenInclude(x => x.StarSystem)
            .Where(x => x.PlayerId == player.Id)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;

        foreach (var operation in operations.Where(x =>
            x.CompletedAt is null &&
            x.CompletesAt <= utcNow))
        {
            ColonizationService.Complete(operation, utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(operations
            .Where(x => x.CompletedAt is null)
            .Select(CreateResponse)
            .ToList());
    }

    private static async Task<IResult> BeginAsync(
        Guid targetPlanetId,
        ColonizePlanetRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Planets)
            .Include(x => x.Ships)
            .Include(x => x.ColonizationOperations)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        if (player.ColonizationOperations.Any(x =>
            x.TargetPlanetId == targetPlanetId))
        {
            return Results.BadRequest(new
            {
                error = "Planet already has a colonization operation."
            });
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

        ColonizationOperation operation;

        try
        {
            operation = ColonizationService.Begin(
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

        dbContext.Ships.Remove(ship);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateResponse(operation));
    }

    private static ColonizationOperationResponse CreateResponse(
        ColonizationOperation operation) =>
        new(
            operation.Id,
            operation.SourcePlanetId,
            operation.TargetPlanetId,
            operation.TargetPlanet.Name,
            operation.TargetPlanet.StarSystem.GalaxyNumber,
            operation.TargetPlanet.StarSystem.SystemNumber,
            operation.TargetPlanet.Position,
            operation.ConsumedShipId,
            operation.ShipName,
            operation.BlueprintName,
            operation.BlueprintVersion,
            operation.StartedAt,
            operation.CompletesAt);
}

public sealed record ColonizePlanetRequest(
    Guid ShipId);

public sealed record ColonizationOperationResponse(
    Guid Id,
    Guid SourcePlanetId,
    Guid TargetPlanetId,
    string TargetPlanetName,
    int Galaxy,
    int System,
    int Position,
    Guid ConsumedShipId,
    string ShipName,
    string BlueprintName,
    int BlueprintVersion,
    DateTime StartedAt,
    DateTime CompletesAt);
