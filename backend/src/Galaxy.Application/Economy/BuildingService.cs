using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class BuildingService
{
    public static ConstructionResult Start(
        Planet planet,
        BuildingType building,
        DateTime utcNow)
    {
        ResourceProductionCalculator.Update(planet, utcNow);

        if (planet.QueuedBuilding is not null)
        {
            throw new InvalidOperationException(
                "A building is already under construction.");
        }

        var currentLevel = GetLevel(planet, building);
        var targetLevel = currentLevel + 1;

        if (currentLevel == 0 &&
            GetUsedSites(planet) >= planet.BuildingSiteCapacity)
        {
            throw new InvalidOperationException(
                "No free building sites.");
        }

        var cost = CalculateCost(building, targetLevel);

        if (planet.Materials < cost.Materials ||
            planet.Deuterium < cost.Deuterium)
        {
            throw new InvalidOperationException(
                "Not enough resources.");
        }

        planet.Materials -= cost.Materials;
        planet.Deuterium -= cost.Deuterium;

        planet.QueuedBuilding = building;
        planet.QueuedBuildingLevel = targetLevel;
        planet.BuildingCompletesAt =
            utcNow.AddSeconds(targetLevel * 10);

        return new ConstructionResult(
            building,
            targetLevel,
            cost,
            planet.BuildingCompletesAt.Value);
    }

    public static bool Complete(
        Planet planet,
        DateTime utcNow)
    {
        if (planet.QueuedBuilding is null ||
            planet.QueuedBuildingLevel is null ||
            planet.BuildingCompletesAt is null ||
            utcNow < planet.BuildingCompletesAt.Value)
        {
            return false;
        }

        var building = planet.QueuedBuilding.Value;
        var targetLevel = planet.QueuedBuildingLevel.Value;
        var completesAt = planet.BuildingCompletesAt.Value;

        ResourceProductionCalculator.Update(
            planet,
            completesAt);

        SetLevel(planet, building, targetLevel);

        planet.QueuedBuilding = null;
        planet.QueuedBuildingLevel = null;
        planet.BuildingCompletesAt = null;

        ResourceProductionCalculator.Update(
            planet,
            utcNow);

        return true;
    }

    public static int GetUsedSites(Planet planet)
    {
        var usedSites = 0;

        if (planet.MaterialsExtractorLevel > 0)
        {
            usedSites++;
        }

        if (planet.DeuteriumExtractorLevel > 0)
        {
            usedSites++;
        }

        if (planet.PowerPlantLevel > 0)
        {
            usedSites++;
        }

        if (planet.WarehouseLevel > 0)
        {
            usedSites++;
        }

        if (planet.ResearchLaboratoryLevel > 0)
        {
            usedSites++;
        }

        if (planet.ProductionComplexLevel > 0)
        {
            usedSites++;
        }

        return usedSites;
    }

    public static BuildingCost CalculateCost(
        BuildingType building,
        int targetLevel)
    {
        if (targetLevel < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetLevel));
        }

        var multiplier = Pow(1.5m, targetLevel - 1);

        return building switch
        {
            BuildingType.MaterialsExtractor =>
                new BuildingCost(
                    decimal.Ceiling(60m * multiplier),
                    0m),

            BuildingType.DeuteriumExtractor =>
                new BuildingCost(
                    decimal.Ceiling(80m * multiplier),
                    decimal.Ceiling(20m * multiplier)),

            BuildingType.PowerPlant =>
                new BuildingCost(
                    decimal.Ceiling(75m * multiplier),
                    decimal.Ceiling(10m * multiplier)),

            BuildingType.Warehouse =>
                new BuildingCost(
                    decimal.Ceiling(100m * multiplier),
                    0m),

            BuildingType.ResearchLaboratory =>
                new BuildingCost(
                    decimal.Ceiling(150m * multiplier),
                    decimal.Ceiling(25m * multiplier)),

            BuildingType.ProductionComplex =>
                new BuildingCost(
                    decimal.Ceiling(200m * multiplier),
                    decimal.Ceiling(50m * multiplier)),

            _ => throw new ArgumentOutOfRangeException(
                nameof(building))
        };
    }

    public static int GetLevel(
        Planet planet,
        BuildingType building)
    {
        return building switch
        {
            BuildingType.MaterialsExtractor =>
                planet.MaterialsExtractorLevel,

            BuildingType.DeuteriumExtractor =>
                planet.DeuteriumExtractorLevel,

            BuildingType.PowerPlant =>
                planet.PowerPlantLevel,

            BuildingType.Warehouse =>
                planet.WarehouseLevel,

            BuildingType.ResearchLaboratory =>
                planet.ResearchLaboratoryLevel,

            BuildingType.ProductionComplex =>
                planet.ProductionComplexLevel,

            _ => throw new ArgumentOutOfRangeException(
                nameof(building))
        };
    }

    private static void SetLevel(
        Planet planet,
        BuildingType building,
        int level)
    {
        switch (building)
        {
            case BuildingType.MaterialsExtractor:
                planet.MaterialsExtractorLevel = level;
                break;

            case BuildingType.DeuteriumExtractor:
                planet.DeuteriumExtractorLevel = level;
                break;

            case BuildingType.PowerPlant:
                planet.PowerPlantLevel = level;
                break;

            case BuildingType.Warehouse:
                planet.WarehouseLevel = level;
                break;

            case BuildingType.ResearchLaboratory:
                planet.ResearchLaboratoryLevel = level;
                break;

            case BuildingType.ProductionComplex:
                planet.ProductionComplexLevel = level;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(building));
        }
    }

    private static decimal Pow(
        decimal value,
        int exponent)
    {
        var result = 1m;

        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }
}

public sealed record BuildingCost(
    decimal Materials,
    decimal Deuterium);

public sealed record ConstructionResult(
    BuildingType Building,
    int TargetLevel,
    BuildingCost Cost,
    DateTime CompletesAt);


