using Galaxy.Application.Production;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ComponentProductionServiceTests
{
    [Fact]
    public void EnqueueAndProcess_ProducesComponents()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var player = CreatePlayer();
        var planet = CreatePlanet(startedAt);

        var result = ComponentProductionService.Enqueue(
            player,
            planet,
            1,
            "humans-engine-1",
            2,
            startedAt);

        Assert.Equal(800m, planet.Materials);
        Assert.Equal(930m, planet.Deuterium);
        Assert.Equal(1, result.QueuePosition);

        Assert.Equal(
            0,
            ComponentProductionService.Process(
                planet,
                startedAt.AddSeconds(29)));

        Assert.Equal(
            1,
            ComponentProductionService.Process(
                planet,
                startedAt.AddSeconds(30)));

        Assert.Empty(planet.ProductionOrders);

        var inventory = Assert.Single(
            planet.ComponentInventory);

        Assert.Equal("humans-engine-1", inventory.ComponentCode);
        Assert.Equal(2, inventory.Quantity);
    }

    [Fact]
    public void Enqueue_RejectsForeignRaceComponent()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        var planet = CreatePlanet(startedAt);

        Assert.Throws<InvalidOperationException>(
            () => ComponentProductionService.Enqueue(
                player,
                planet,
                1,
                "synthetics-engine-1",
                1,
                startedAt));
    }

    [Fact]
    public void Enqueue_AllowsUniversalComponent()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        var planet = CreatePlanet(startedAt);

        var result = ComponentProductionService.Enqueue(
            player,
            planet,
            1,
            "ENG-01",
            1,
            startedAt);

        Assert.Equal("ENG-01", result.ComponentCode);
        Assert.Equal(840m, planet.Materials);
        Assert.Equal(980m, planet.Deuterium);
    }

    [Fact]
    public void Enqueue_RequiresOwnRaceAndLocalRaceComplex()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.ShipEngineering, 3);
        var planet = CreatePlanet(startedAt);
        planet.Materials = 5000m;

        var missingBuilding = Assert.Throws<InvalidOperationException>(
            () => ComponentProductionService.Enqueue(
                player, planet, 1, "ARM-H01", 1, startedAt));
        Assert.Contains("race engineering complex", missingBuilding.Message);

        planet.RaceEngineeringComplexLevel = 1;
        var result = ComponentProductionService.Enqueue(
            player, planet, 1, "ARM-H01", 1, startedAt);
        Assert.Equal("ARM-H01", result.ComponentCode);

        Assert.Throws<InvalidOperationException>(
            () => ComponentProductionService.Enqueue(
                player, planet, 1, "RCT-S01", 1, startedAt));
    }

    [Fact]
    public void Enqueue_RaceComplexLevelTwoAcceleratesUniqueComponents()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.ShipEngineering, 3);
        var planet = CreatePlanet(startedAt);
        planet.Materials = 5000m;
        planet.RaceEngineeringComplexLevel = 2;

        var result = ComponentProductionService.Enqueue(
            player, planet, 1, "ARM-H01", 1, startedAt);

        Assert.Equal(startedAt.AddSeconds(594), result.CompletesAt);
    }

    [Fact]
    public void Enqueue_RejectsFutureQuantumDamper()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.Electronics, 4);
        var planet = CreatePlanet(startedAt);

        var error = Assert.Throws<InvalidOperationException>(() =>
            ComponentProductionService.Enqueue(
                player,
                planet,
                1,
                "QDM-01",
                1,
                startedAt));

        Assert.Equal(
            "Component is reserved for future archaeology content.",
            error.Message);
    }

    private static Player CreatePlayer()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander",
            Race = RaceType.Humans
        };

        player.Technologies.Add(new PlayerTechnology
        {
            PlayerId = player.Id,
            Player = player,
            Technology = TechnologyType.EngineSystems,
            Level = 1
        });

        return player;
    }

    private static void AddTechnology(
        Player player,
        TechnologyType technology,
        int level)
    {
        player.Technologies.Add(new PlayerTechnology
        {
            PlayerId = player.Id,
            Player = player,
            Technology = technology,
            Level = level
        });
    }

    private static Planet CreatePlanet(DateTime startedAt)
    {
        return new Planet
        {
            Id = Guid.NewGuid(),
            Materials = 1000m,
            Deuterium = 1000m,
            ProductionComplexLevel = 1,
            ResourcesUpdatedAt = startedAt
        };
    }
}
