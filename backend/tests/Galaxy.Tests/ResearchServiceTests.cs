using Galaxy.Application.Research;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ResearchServiceTests
{
    [Fact]
    public void StartAndComplete_ResearchesTechnologyInPlanetStream()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var player = CreatePlayer();
        var planet = CreatePlanet(player, startedAt, laboratoryLevel: 1);

        var research = ResearchService.Start(
            player,
            planet,
            TechnologyType.MaterialsScience,
            startedAt);

        Assert.Equal(1, research.TargetLevel);
        Assert.Equal(1, research.StreamNumber);
        Assert.Equal(99_700m, planet.Materials);
        Assert.Single(player.ResearchOrders);

        Assert.Empty(ResearchService.Complete(
            player,
            startedAt.AddMinutes(4)));

        Assert.Single(ResearchService.Complete(
            player,
            startedAt.AddMinutes(5)));
        Assert.Equal(
            1,
            ResearchService.GetLevel(
                player,
                TechnologyType.MaterialsScience));
        Assert.Empty(player.ResearchOrders);
        Assert.Empty(planet.ResearchOrders);
    }

    [Fact]
    public void Start_RejectsMissingCrossBranchPrerequisites()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        var planet = CreatePlanet(player, startedAt, laboratoryLevel: 5);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ResearchService.Start(
                player,
                planet,
                TechnologyType.EngineSystems,
                startedAt));

        Assert.StartsWith("Missing prerequisites:", exception.Message);
    }

    [Fact]
    public void Start_UsesTwoStreamsAfterCoordinationAndLaboratoryFive()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.MaterialsScience, 1);
        AddTechnology(player, TechnologyType.EnergySystems, 1);
        AddTechnology(player, TechnologyType.ResearchCoordination, 1);
        var planet = CreatePlanet(player, startedAt, laboratoryLevel: 5);

        var first = ResearchService.Start(
            player,
            planet,
            TechnologyType.ShipEngineering,
            startedAt);
        var second = ResearchService.Start(
            player,
            planet,
            TechnologyType.EngineSystems,
            startedAt);

        Assert.Equal(1, first.StreamNumber);
        Assert.Equal(2, second.StreamNumber);
        Assert.Equal(2, planet.ResearchOrders.Count);
    }

    [Fact]
    public void Start_ProvidesOneIndependentStreamPerResearchPlanet()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.MaterialsScience, 1);
        AddTechnology(player, TechnologyType.EnergySystems, 1);
        var homeworld = CreatePlanet(player, startedAt, laboratoryLevel: 1);
        var colony = CreatePlanet(player, startedAt, laboratoryLevel: 1);

        var homeworldResearch = ResearchService.Start(
            player,
            homeworld,
            TechnologyType.ShipEngineering,
            startedAt);
        var colonyResearch = ResearchService.Start(
            player,
            colony,
            TechnologyType.EngineSystems,
            startedAt);

        Assert.Equal(1, homeworldResearch.StreamNumber);
        Assert.Equal(1, colonyResearch.StreamNumber);
        Assert.Single(homeworld.ResearchOrders);
        Assert.Single(colony.ResearchOrders);
        Assert.Equal(2, player.ResearchOrders.Count);
    }

    [Fact]
    public void AvailableStreams_RequiresGlobalTechnologyAndPlanetLaboratory()
    {
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.ResearchCoordination, 2);

        var laboratoryFive = CreatePlanet(player, DateTime.UtcNow, 5);
        var laboratoryNine = CreatePlanet(player, DateTime.UtcNow, 9);

        Assert.Equal(2, ResearchService.GetAvailableStreamCount(player, laboratoryFive));
        Assert.Equal(3, ResearchService.GetAvailableStreamCount(player, laboratoryNine));
    }

    [Fact]
    public void Start_UsesThreeStreamsAfterCoordinationTwoAndLaboratoryNine()
    {
        var startedAt = DateTime.UtcNow;
        var player = CreatePlayer();
        AddTechnology(player, TechnologyType.MaterialsScience, 1);
        AddTechnology(player, TechnologyType.EnergySystems, 1);
        AddTechnology(player, TechnologyType.ComputingSystems, 1);
        AddTechnology(player, TechnologyType.ResearchCoordination, 2);
        var planet = CreatePlanet(player, startedAt, laboratoryLevel: 9);

        var first = ResearchService.Start(
            player,
            planet,
            TechnologyType.ShipEngineering,
            startedAt);
        var second = ResearchService.Start(
            player,
            planet,
            TechnologyType.EngineSystems,
            startedAt);
        var third = ResearchService.Start(
            player,
            planet,
            TechnologyType.LaserSystems,
            startedAt);

        Assert.Equal(1, first.StreamNumber);
        Assert.Equal(2, second.StreamNumber);
        Assert.Equal(3, third.StreamNumber);
        Assert.Equal(3, planet.ResearchOrders.Count);
    }

    private static Player CreatePlayer() => new()
    {
        Id = Guid.NewGuid(),
        Username = "Commander"
    };

    private static Planet CreatePlanet(
        Player player,
        DateTime startedAt,
        int laboratoryLevel) => new()
    {
        Id = Guid.NewGuid(),
        PlayerId = player.Id,
        Player = player,
        Materials = 100_000m,
        Deuterium = 100_000m,
        ResearchLaboratoryLevel = laboratoryLevel,
        ResourcesUpdatedAt = startedAt
    };

    private static void AddTechnology(
        Player player,
        TechnologyType technology,
        int level) => player.Technologies.Add(new PlayerTechnology
    {
        Id = Guid.NewGuid(),
        PlayerId = player.Id,
        Player = player,
        Technology = technology,
        Level = level
    });
}
