using Galaxy.Application.Components;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.Colonization;

public static class ColonizationService
{
    public static ColonizationResult Colonize(
        Player player,
        Ship ship,
        Planet targetPlanet,
        DateTime utcNow)
    {
        if (ship.PlayerId != player.Id)
        {
            throw new InvalidOperationException(
                "Ship does not belong to the player.");
        }

        if (targetPlanet.PlayerId is not null)
        {
            throw new InvalidOperationException(
                "Planet is already colonized.");
        }

        if (ship.Planet.StarSystemId != targetPlanet.StarSystemId)
        {
            throw new InvalidOperationException(
                "Colonization is currently limited to the same star system.");
        }

        var hasColonyModule = ship.Blueprint.Modules.Any(module =>
            module.Quantity > 0 &&
            StarterComponentCatalog.Find(module.ComponentCode)
                is ColonyModuleDefinition);

        if (!hasColonyModule)
        {
            throw new InvalidOperationException(
                "Ship does not have a colony module.");
        }

        targetPlanet.PlayerId = player.Id;
        targetPlanet.Player = player;
        targetPlanet.Name =
            $"Colony {targetPlanet.StarSystem.SystemNumber}:" +
            $"{targetPlanet.Position}";

        targetPlanet.Materials = 250m;
        targetPlanet.Deuterium = 50m;
        targetPlanet.MaterialsExtractorLevel = 1;
        targetPlanet.DeuteriumExtractorLevel = 0;
        targetPlanet.PowerPlantLevel = 1;
        targetPlanet.WarehouseLevel = 1;
        targetPlanet.ResearchLaboratoryLevel = 0;
        targetPlanet.ProductionComplexLevel = 0;
        targetPlanet.AssemblyComplexLevel = 0;
        targetPlanet.ResourcesUpdatedAt = utcNow;

        if (!player.Planets.Contains(targetPlanet))
        {
            player.Planets.Add(targetPlanet);
        }

        ship.Planet.Ships.Remove(ship);
        player.Ships.Remove(ship);

        return new ColonizationResult(
            targetPlanet,
            ship);
    }
}

public sealed record ColonizationResult(
    Planet Planet,
    Ship ConsumedShip);
