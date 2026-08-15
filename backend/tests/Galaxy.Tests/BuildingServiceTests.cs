using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class BuildingServiceTests
{
    [Fact]
    public void StartAndComplete_UpgradesBuilding()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = CreatePlanet(startedAt);
        planet.BuildingSiteCapacity = 4;

        var construction = BuildingService.Start(
            planet,
            BuildingType.MaterialsExtractor,
            startedAt);

        Assert.Equal(2, construction.TargetLevel);
        Assert.Equal(910m, planet.Materials);
        Assert.Equal(
            BuildingType.MaterialsExtractor,
            planet.QueuedBuilding);

        Assert.False(BuildingService.Complete(
            planet,
            startedAt.AddSeconds(19)));

        Assert.True(BuildingService.Complete(
            planet,
            startedAt.AddSeconds(21)));

        Assert.Equal(2, planet.MaterialsExtractorLevel);
        Assert.Null(planet.QueuedBuilding);
        Assert.Null(planet.BuildingCompletesAt);
    }

    [Fact]
    public void Start_RejectsNewBuildingWhenSitesAreFull()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var planet = CreatePlanet(startedAt);
        planet.BuildingSiteCapacity = 3;

        var exception = Assert.Throws<InvalidOperationException>(
            () => BuildingService.Start(
                planet,
                BuildingType.DeuteriumExtractor,
                startedAt));

        Assert.Equal(
            "No free building sites.",
            exception.Message);
    }

    [Fact]
    public void StartAndComplete_BuildsRaceEngineeringComplex()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var planet = CreatePlanet(startedAt);
        planet.BuildingSiteCapacity = 4;

        var construction = BuildingService.Start(
            planet,
            BuildingType.RaceEngineeringComplex,
            startedAt);

        Assert.Equal(1, construction.TargetLevel);
        Assert.Equal(100m, planet.Materials);
        Assert.Equal(750m, planet.Deuterium);
        Assert.True(BuildingService.Complete(
            planet,
            startedAt.AddSeconds(11)));
        Assert.Equal(1, planet.RaceEngineeringComplexLevel);
        Assert.Equal(4, BuildingService.GetUsedSites(planet));
    }

    private static Planet CreatePlanet(DateTime startedAt)
    {
        return new Planet
        {
            Materials = 1000m,
            Deuterium = 1000m,
            MaterialsExtractorLevel = 1,
            PowerPlantLevel = 1,
            WarehouseLevel = 1,
            ResourcesUpdatedAt = startedAt
        };
    }
}
