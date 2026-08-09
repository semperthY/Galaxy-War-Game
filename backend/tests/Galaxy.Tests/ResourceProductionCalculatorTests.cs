using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ResourceProductionCalculatorTests
{
    [Fact]
    public void Production_GrowsWithLevelAndGrowthFactor()
    {
        Assert.Equal(
            40m,
            ResourceProductionCalculator.CalculateMaterialsPerHour(1));

        Assert.Equal(
            422.9620m,
            ResourceProductionCalculator.CalculateMaterialsPerHour(6));

        Assert.Equal(
            158.6107m,
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

        Assert.Equal(80m, planet.Materials);
        Assert.Equal(30m, planet.Deuterium);
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

        Assert.Equal(104.1669m, planet.Materials);
        Assert.Equal(15.5702m, planet.Deuterium);
    }

    [Fact]
    public void Update_DoesNotExceedStorageCapacity()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = new Planet
        {
            Materials = 990m,
            Deuterium = 495m,
            MaterialsExtractorLevel = 1,
            DeuteriumExtractorLevel = 1,
            PowerPlantLevel = 1,
            WarehouseLevel = 0,
            ResourcesUpdatedAt = startedAt
        };

        ResourceProductionCalculator.Update(
            planet,
            startedAt.AddHours(1));

        Assert.Equal(1000m, planet.Materials);
        Assert.Equal(500m, planet.Deuterium);
    }
}

