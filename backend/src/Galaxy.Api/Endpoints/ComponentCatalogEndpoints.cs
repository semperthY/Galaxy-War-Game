using Galaxy.Application.Components;
using Galaxy.Api.Security;
using Galaxy.Application.Research;
using Galaxy.Domain.ShipDesign;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ComponentCatalogEndpoints
{
    public static void MapComponentCatalogEndpoints(
        this WebApplication app)
    {
        app.MapGet(
            "/api/game/components",
            GetAllAsync)
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

        var player = await dbContext.Players
            .Include(x => x.Technologies)
            .SingleOrDefaultAsync(x => x.Id == playerId, cancellationToken);

        if (player is null)
        {
            return Results.NotFound();
        }

        var components = StarterComponentCatalog
            .GetAll()
            .Select(component =>
                ComponentCatalogResponseFactory.Create(
                    player,
                    component))
            .ToList();

        return Results.Ok(components);
    }

    private static object CreateResponse(
        Galaxy.Domain.Entities.Player player,
        IComponentDefinition component)
    {
        var canManufacture =
            component.Race is null ||
            component.Race == player.Race;

        var technologyUnlocked =
            ResearchService.GetLevel(
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
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
                canManufacture,
                technologyUnlocked,
                missile.ShieldDamage,
                missile.HullDamage,
                missile.EnergyConsumption,
                missile.CommandLoad
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(component))
        };
    }
}
