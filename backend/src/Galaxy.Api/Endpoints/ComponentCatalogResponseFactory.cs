using Galaxy.Application.Components;
using Galaxy.Application.Research;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Api.Endpoints;

internal static class ComponentCatalogResponseFactory
{
    public static Dictionary<string, object?> Create(
        Player player,
        IComponentDefinition component,
        Planet? planet = null)
    {
        var details = ComponentCatalogDetails.Get(component.Code);
        var raceAllowed = component.Race is null ||
            component.Race == player.Race;
        var technologyUnlocked = ResearchService.GetLevel(
                player,
                component.RequiredTechnology) >=
            component.RequiredTechnologyLevel;
        var buildingAllowed = planet is null ||
            planet.RaceEngineeringComplexLevel >=
            details.RequiredRaceComplexLevel;
        var currentlyAvailable =
            StarterComponentCatalog.IsCurrentlyAvailable(component);

        var response = new Dictionary<string, object?>
        {
            ["code"] = component.Code,
            ["name"] = component.Name,
            ["race"] = component.Race,
            ["type"] = component.Type,
            ["cost"] = component.Cost,
            ["productionSeconds"] = component.ProductionSeconds,
            ["requiredTechnology"] = component.RequiredTechnology,
            ["requiredTechnologyLevel"] = component.RequiredTechnologyLevel,
            ["requiredRaceComplexLevel"] = details.RequiredRaceComplexLevel,
            ["shortDescription"] = details.ShortDescription,
            ["bestFor"] = details.BestFor,
            ["tradeoff"] = details.Tradeoff,
            ["technologyUnlocked"] = technologyUnlocked && currentlyAvailable,
            ["unlocked"] = technologyUnlocked && currentlyAvailable,
            ["canManufacture"] = currentlyAvailable && raceAllowed && buildingAllowed,
            ["canInstall"] = currentlyAvailable,
            ["futureContent"] = !currentlyAvailable,
            ["manufacturingBlockReason"] = GetBlockReason(
                currentlyAvailable,
                raceAllowed,
                technologyUnlocked,
                buildingAllowed,
                component,
                details)
        };

        AddStatistics(response, component);
        return response;
    }

    private static string? GetBlockReason(
        bool currentlyAvailable,
        bool raceAllowed,
        bool technologyUnlocked,
        bool buildingAllowed,
        IComponentDefinition component,
        ComponentDetails details)
    {
        if (!currentlyAvailable)
        {
            return "Источник: археология — функция появится в будущей версии.";
        }

        if (!raceAllowed)
        {
            return "Уникальная модель другой расы.";
        }

        if (!technologyUnlocked)
        {
            return $"Требуется {component.RequiredTechnology} " +
                $"ур. {component.RequiredTechnologyLevel}.";
        }

        if (!buildingAllowed)
        {
            return "Требуется расовый инженерный комплекс " +
                $"ур. {details.RequiredRaceComplexLevel}.";
        }

        return null;
    }

    private static void AddStatistics(
        IDictionary<string, object?> response,
        IComponentDefinition component)
    {
        switch (component)
        {
            case HullDefinition hull:
                response["capacity"] = hull.Capacity;
                response["structuralIntegrity"] = hull.StructuralIntegrity;
                break;
            case EngineDefinition engine:
                AddVolume(response, engine.Volume);
                response["inSystemSpeed"] = engine.InSystemSpeed;
                response["interSystemSpeed"] = engine.InterSystemSpeed;
                response["energyConsumption"] = engine.EnergyConsumption;
                break;
            case ReactorDefinition reactor:
                AddVolume(response, reactor.Volume);
                response["energyOutput"] = reactor.EnergyOutput;
                break;
            case ControlSystemDefinition control:
                AddVolume(response, control.Volume);
                response["commandRating"] = control.CommandRating;
                response["energyConsumption"] = control.EnergyConsumption;
                break;
            case ColonyModuleDefinition colony:
                AddVolume(response, colony.Volume);
                response["energyConsumption"] = colony.EnergyConsumption;
                break;
            case ArmorDefinition armor:
                AddVolume(response, armor.Volume);
                response["bonusStructuralIntegrity"] =
                    armor.BonusStructuralIntegrity;
                break;
            case ShieldDefinition shield:
                AddVolume(response, shield.Volume);
                response["shieldCapacity"] = shield.ShieldCapacity;
                response["energyConsumption"] = shield.EnergyConsumption;
                break;
            case ScannerDefinition scanner:
                AddVolume(response, scanner.Volume);
                response["scanRange"] = scanner.ScanRange;
                response["energyConsumption"] = scanner.EnergyConsumption;
                response["commandLoad"] = scanner.CommandLoad;
                break;
            case CargoHoldDefinition cargo:
                AddVolume(response, cargo.Volume);
                response["cargoCapacity"] = cargo.CargoCapacity;
                response["energyConsumption"] = cargo.EnergyConsumption;
                break;
            case MiningModuleDefinition mining:
                AddVolume(response, mining.Volume);
                response["miningRatePerMinute"] = mining.MiningRatePerMinute;
                response["energyConsumption"] = mining.EnergyConsumption;
                break;
            case LaserWeaponDefinition laser:
                AddWeapon(response, laser.Volume, laser.ShieldDamage,
                    laser.HullDamage, laser.EnergyConsumption,
                    laser.CommandLoad);
                break;
            case MissileWeaponDefinition missile:
                AddWeapon(response, missile.Volume, missile.ShieldDamage,
                    missile.HullDamage, missile.EnergyConsumption,
                    missile.CommandLoad);
                break;
            case QuantumDamperDefinition damper:
                AddVolume(response, damper.Volume);
                response["volumeReduction"] = damper.VolumeReduction;
                response["energyReduction"] = damper.EnergyReduction;
                break;
        }
    }

    private static void AddVolume(
        IDictionary<string, object?> response,
        decimal volume) => response["volume"] = volume;

    private static void AddWeapon(
        IDictionary<string, object?> response,
        decimal volume,
        decimal shieldDamage,
        decimal hullDamage,
        decimal energyConsumption,
        decimal commandLoad)
    {
        AddVolume(response, volume);
        response["shieldDamage"] = shieldDamage;
        response["hullDamage"] = hullDamage;
        response["energyConsumption"] = energyConsumption;
        response["commandLoad"] = commandLoad;
    }
}
