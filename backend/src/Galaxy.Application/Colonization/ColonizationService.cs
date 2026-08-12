using Galaxy.Application.Components;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.Colonization;

public static class ColonizationService
{
    public static readonly TimeSpan DeploymentDuration =
        TimeSpan.FromMinutes(30);

    public static ColonizationOperation Begin(
        Player player,
        Ship ship,
        Planet targetPlanet,
        DateTime utcNow)
    {
        Validate(player, ship, targetPlanet);

        var operation = new ColonizationOperation
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            SourcePlanetId = ship.PlanetId,
            TargetPlanetId = targetPlanet.Id,
            TargetPlanet = targetPlanet,
            ConsumedShipId = ship.Id,
            ShipName = ship.Name,
            BlueprintName = ship.Blueprint.Name,
            BlueprintVersion = ship.Blueprint.Version,
            StartedAt = utcNow,
            CompletesAt = utcNow.Add(DeploymentDuration)
        };

        player.ColonizationOperations.Add(operation);
        ship.Planet.Ships.Remove(ship);
        player.Ships.Remove(ship);

        return operation;
    }

    public static Planet Complete(
        ColonizationOperation operation,
        DateTime utcNow)
    {
        if (operation.CompletedAt is not null)
        {
            return operation.TargetPlanet;
        }

        if (utcNow < operation.CompletesAt)
        {
            throw new InvalidOperationException(
                "Colonization deployment is not complete yet.");
        }

        var targetPlanet = operation.TargetPlanet;

        if (targetPlanet.PlayerId is not null &&
            targetPlanet.PlayerId != operation.PlayerId)
        {
            throw new InvalidOperationException(
                "Planet is already colonized.");
        }

        if (targetPlanet.PlayerId is null)
        {
            targetPlanet.PlayerId = operation.PlayerId;
            targetPlanet.Player = operation.Player;
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
            targetPlanet.QueuedBuilding = null;
            targetPlanet.QueuedBuildingLevel = null;
            targetPlanet.BuildingCompletesAt = null;
            targetPlanet.ResourcesUpdatedAt = utcNow;

            if (!operation.Player.Planets.Contains(targetPlanet))
            {
                operation.Player.Planets.Add(targetPlanet);
            }
        }

        operation.CompletedAt = utcNow;
        return targetPlanet;
    }

    private static void Validate(
        Player player,
        Ship ship,
        Planet targetPlanet)
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
    }
}
