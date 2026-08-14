using Galaxy.Application.Components;
using Galaxy.Api.Security;
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
        var group = app.MapGroup("/api/game/production")
            .RequireAuthorization();

        group.MapGet("/", GetStatusAsync);
        group.MapPost(
            "/lines/{lineNumber:int}/orders",
            EnqueueAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        Guid? planetId,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            planetId,
            httpContext,
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
        Guid? planetId,
        int lineNumber,
        CreateProductionOrderRequest request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            planetId,
            httpContext,
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
        Guid? planetId,
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
            return null;
        }

        var player = await dbContext.Players
            .Include(x => x.Technologies)
            .SingleOrDefaultAsync(x => x.Id == playerId, cancellationToken);

        if (player is null)
        {
            return null;
        }

        var planet = await dbContext.Planets
            .Include(x => x.ComponentInventory)
            .Include(x => x.ProductionOrders)
            .SelectOwnedPlanet(player.Id, planetId)
            .FirstOrDefaultAsync(cancellationToken);

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

            ColonyModuleDefinition colony => new
            {
                colony.Code,
                colony.Name,
                colony.Race,
                colony.Type,
                colony.Volume,
                colony.Cost,
                colony.ProductionSeconds,
                colony.RequiredTechnology,
                colony.RequiredTechnologyLevel,
                unlocked,
                colony.EnergyConsumption
            },

            ArmorDefinition armor => new
            {
                armor.Code,
                armor.Name,
                armor.Race,
                armor.Type,
                armor.Volume,
                armor.Cost,
                armor.ProductionSeconds,
                armor.RequiredTechnology,
                armor.RequiredTechnologyLevel,
                unlocked,
                armor.BonusStructuralIntegrity
            },

            ShieldDefinition shield => new
            {
                shield.Code,
                shield.Name,
                shield.Race,
                shield.Type,
                shield.Volume,
                shield.Cost,
                shield.ProductionSeconds,
                shield.RequiredTechnology,
                shield.RequiredTechnologyLevel,
                unlocked,
                shield.ShieldCapacity,
                shield.EnergyConsumption
            },

            ScannerDefinition scanner => new
            {
                scanner.Code,
                scanner.Name,
                scanner.Race,
                scanner.Type,
                scanner.Volume,
                scanner.Cost,
                scanner.ProductionSeconds,
                scanner.RequiredTechnology,
                scanner.RequiredTechnologyLevel,
                unlocked,
                scanner.ScanRange,
                scanner.EnergyConsumption,
                scanner.CommandLoad
            },

            CargoHoldDefinition cargo => new
            {
                cargo.Code,
                cargo.Name,
                cargo.Race,
                cargo.Type,
                cargo.Volume,
                cargo.Cost,
                cargo.ProductionSeconds,
                cargo.RequiredTechnology,
                cargo.RequiredTechnologyLevel,
                unlocked,
                cargo.CargoCapacity,
                cargo.EnergyConsumption
            },

            MiningModuleDefinition mining => new
            {
                mining.Code,
                mining.Name,
                mining.Race,
                mining.Type,
                mining.Volume,
                mining.Cost,
                mining.ProductionSeconds,
                mining.RequiredTechnology,
                mining.RequiredTechnologyLevel,
                unlocked,
                mining.MiningRatePerMinute,
                mining.EnergyConsumption
            },

            LaserWeaponDefinition laser => new
            {
                laser.Code,
                laser.Name,
                laser.Race,
                laser.Type,
                laser.Volume,
                laser.Cost,
                laser.ProductionSeconds,
                laser.RequiredTechnology,
                laser.RequiredTechnologyLevel,
                unlocked,
                laser.ShieldDamage,
                laser.HullDamage,
                laser.EnergyConsumption,
                laser.CommandLoad
            },

            MissileWeaponDefinition missile => new
            {
                missile.Code,
                missile.Name,
                missile.Race,
                missile.Type,
                missile.Volume,
                missile.Cost,
                missile.ProductionSeconds,
                missile.RequiredTechnology,
                missile.RequiredTechnologyLevel,
                unlocked,
                missile.ShieldDamage,
                missile.HullDamage,
                missile.EnergyConsumption,
                missile.CommandLoad
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





