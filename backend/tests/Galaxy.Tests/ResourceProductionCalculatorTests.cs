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
            MetalMineLevel = 1,
            CrystalMineLevel = 1,
            DeuteriumMineLevel = 1,
            ResourcesUpdatedAt = startedAt
        };

        ResourceProductionCalculator.Update(
            planet,
            startedAt.AddHours(2));

        Assert.Equal(60m, planet.Metal);
        Assert.Equal(40m, planet.Crystal);
        Assert.Equal(20m, planet.Deuterium);
        Assert.Equal(startedAt.AddHours(2), planet.ResourcesUpdatedAt);
    }
}
