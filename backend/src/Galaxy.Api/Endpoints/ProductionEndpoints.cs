using Galaxy.Application.Components;
using Galaxy.Application.Production;
using Galaxy.Application.Research;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ProductionEndpoints
{
    public static void MapProductionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game/production");

        group.MapGet("/", GetStatusAsync);
        group.MapPost(
            "/lines/{lineNumber:int}/orders",
            EnqueueAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        ComponentProductionService.Process(
            state.Planet,
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(
            state.Player,
            state.Planet));
    }

    private static async Task<IResult> EnqueueAsync(
        int lineNumber,
        CreateProductionOrderRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        try
        {
            ComponentProductionService.Enqueue(
                state.Player,
                state.Planet,
                lineNumber,
                request.ComponentCode,
                request.Quantity,
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

        return Results.Ok(CreateStatus(
            state.Player,
            state.Planet));
    }

    private static async Task<ProductionState?> LoadStateAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Technologies)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return null;
        }

        var planet = await dbContext.Planets
            .Include(x => x.ComponentInventory)
            .Include(x => x.ProductionOrders)
            .SingleOrDefaultAsync(
                x => x.PlayerId == player.Id,
                cancellationToken);

        return planet is null
            ? null
            : new ProductionState(player, planet);
    }

    private static ProductionStatusResponse CreateStatus(
        Player player,
        Planet planet)
    {
        var catalog = StarterComponentCatalog
            .GetForRace(player.Race)
            .Select(component =>
                CreateCatalogItem(player, component))
            .ToList();

        var inventory = planet.ComponentInventory
            .OrderBy(x => x.ComponentCode)
            .Select(x => new ComponentInventoryResponse(
                x.ComponentCode,
                x.Quantity))
            .ToList();

        var orders = planet.ProductionOrders
            .OrderBy(x => x.LineNumber)
            .ThenBy(x => x.QueuePosition)
            .Select(x => new ProductionOrderResponse(
                x.LineNumber,
                x.QueuePosition,
                x.ComponentCode,
                x.Quantity,
                x.StartedAt,
                x.CompletesAt))
            .ToList();

        return new ProductionStatusResponse(
            planet.Materials,
            planet.Deuterium,
            planet.ProductionComplexLevel,
            ComponentProductionService.GetLineCount(planet),
            catalog,
            inventory,
            orders);
    }

    private static object CreateCatalogItem(
        Player player,
        IComponentDefinition component)
    {
        var unlocked = ResearchService.GetLevel(
                player,
                component.RequiredTechnology) >=
            component.RequiredTechnologyLevel;

        return component switch
        {
            HullDefinition hull => new
            {
                hull.Code,
                hull.Name,
                hull.Race,
                hull.Type,
                hull.Cost,
                hull.ProductionSeconds,
                hull.RequiredTechnology,
                hull.RequiredTechnologyLevel,
                unlocked,
                hull.Capacity,
                hull.StructuralIntegrity
            },

            EngineDefinition engine => new
            {
                engine.Code,
                engine.Name,
                engine.Race,
                engine.Type,
                engine.Volume,
                engine.Cost,
                engine.ProductionSeconds,
                engine.RequiredTechnology,
                engine.RequiredTechnologyLevel,
                unlocked,
                engine.InSystemSpeed,
                engine.InterSystemSpeed,
                engine.EnergyConsumption
            },

            ReactorDefinition reactor => new
            {
                reactor.Code,
                reactor.Name,
                reactor.Race,
                reactor.Type,
                reactor.Volume,
                reactor.Cost,
                reactor.ProductionSeconds,
                reactor.RequiredTechnology,
                reactor.RequiredTechnologyLevel,
                unlocked,
                reactor.EnergyOutput
            },

            ControlSystemDefinition control => new
            {
                control.Code,
                control.Name,
                control.Race,
                control.Type,
                control.Volume,
                control.Cost,
                control.ProductionSeconds,
                control.RequiredTechnology,
                control.RequiredTechnologyLevel,
                unlocked,
                control.CommandRating,
                control.EnergyConsumption
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(component))
        };
    }

    private sealed record ProductionState(
        Player Player,
        Planet Planet);
}

public sealed record CreateProductionOrderRequest(
    string ComponentCode,
    int Quantity);

public sealed record ProductionStatusResponse(
    decimal Materials,
    decimal Deuterium,
    int ProductionComplexLevel,
    int LineCount,
    List<object> Catalog,
    List<ComponentInventoryResponse> Inventory,
    List<ProductionOrderResponse> Orders);

public sealed record ComponentInventoryResponse(
    string ComponentCode,
    int Quantity);

public sealed record ProductionOrderResponse(
    int LineNumber,
    int QueuePosition,
    string ComponentCode,
    int Quantity,
    DateTime? StartedAt,
    DateTime? CompletesAt);

