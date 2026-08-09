using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ShipBlueprintEndpoints
{
    public static void MapShipBlueprintEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/game/blueprints");

        group.MapGet("/", GetAllAsync);
        group.MapPost("/", CreateAsync);
    }

    private static async Task<IResult> GetAllAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .Include(x => x.Blueprints)
            .ThenInclude(x => x.Modules)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        var blueprints = player.Blueprints
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version)
            .Select(CreateResponse)
            .ToList();

        return Results.Ok(blueprints);
    }

    private static async Task<IResult> CreateAsync(
        CreateShipBlueprintRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Blueprints)
            .ThenInclude(x => x.Modules)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        ShipBlueprintCreationResult result;

        try
        {
            result = ShipBlueprintService.Create(
                player,
                request.Name,
                request.HullCode,
                request.Modules,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/game/blueprints/{result.Blueprint.Id}",
            CreateResponse(result.Blueprint));
    }

    private static ShipBlueprintResponse CreateResponse(
        ShipBlueprint blueprint)
    {
        var modules = blueprint.Modules
            .Select(x => new ModuleSelection(
                x.ComponentCode,
                x.Quantity))
            .ToList();

        var design = ShipDesignCalculator.Calculate(
            blueprint.HullCode,
            modules);

        return new ShipBlueprintResponse(
            blueprint.Id,
            blueprint.Name,
            blueprint.Version,
            blueprint.HullCode,
            blueprint.CreatedAt,
            modules,
            design);
    }
}

public sealed record CreateShipBlueprintRequest(
    string Name,
    string HullCode,
    IReadOnlyCollection<ModuleSelection> Modules);

public sealed record ShipBlueprintResponse(
    Guid Id,
    string Name,
    int Version,
    string HullCode,
    DateTime CreatedAt,
    IReadOnlyCollection<ModuleSelection> Modules,
    ShipDesignResult Design);
