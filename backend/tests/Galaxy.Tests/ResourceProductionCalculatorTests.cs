using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ResourceProductionCalculatorTests
{
    [Fact]
    public void Production_GrowsWithLevelAndGrowthFactor()
    {
        Assert.Equal(
            100m,
            ResourceProductionCalculator.CalculateMaterialsPerHour(1));

        Assert.Equal(
            1057.4050m,
            ResourceProductionCalculator.CalculateMaterialsPerHour(6));

        Assert.Equal(
            370.0917m,
            ResourceProductionCalculator.CalculateDeuteriumPerHour(6));
    }

    [Fact]
    public void Update_ProducesResourcesForElapsedTime()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = new Planet
        {
            MaterialsExtractorLevel = 1,
            DeuteriumExtractorLevel = 1,
            PowerPlantLevel = 1,
            ResourcesUpdatedAt = startedAt
        };

        ResourceProductionCalculator.Update(
            planet,
            startedAt.AddHours(2));

        Assert.Equal(200m, planet.Materials);
        Assert.Equal(70m, planet.Deuterium);
    }

    [Fact]
    public void Update_ReducesProductionWhenEnergyIsInsufficient()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = new Planet
        {
            MaterialsExtractorLevel = 4,
            DeuteriumExtractorLevel = 2,
            PowerPlantLevel = 1,
            ResourcesUpdatedAt = startedAt
        };

        ResourceProductionCalculator.Update(
            planet,
            startedAt.AddHours(1));

        Assert.InRange(planet.Materials, 250m, 270m);
        Assert.InRange(planet.Deuterium, 35m, 40m);
    }

    [Fact]
    public void Update_DoesNotExceedStorageCapacity()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = new Planet
        {
            Materials = 1490m,
            Deuterium = 745m,
            MaterialsExtractorLevel = 1,
            DeuteriumExtractorLevel = 1,
            PowerPlantLevel = 1,
            WarehouseLevel = 0,
            ResourcesUpdatedAt = startedAt
        };

        ResourceProductionCalculator.Update(
            planet,
            startedAt.AddHours(1));

        Assert.Equal(1500m, planet.Materials);
        Assert.Equal(750m, planet.Deuterium);
    }
}
