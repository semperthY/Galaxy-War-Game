using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ResourceProductionCalculatorTests
{
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

        Assert.Equal(60m, planet.Materials);
        Assert.Equal(20m, planet.Deuterium);
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

        Assert.Equal(60m, planet.Materials);
        Assert.Equal(10m, planet.Deuterium);
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

