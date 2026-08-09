using Galaxy.Application.Research;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ResearchServiceTests
{
    [Fact]
    public void StartAndComplete_ResearchesTechnology()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander"
        };

        var planet = CreatePlanet(startedAt);

        var research = ResearchService.Start(
            player,
            planet,
            TechnologyType.EnergySystems,
            startedAt);

        Assert.Equal(1, research.TargetLevel);
        Assert.Equal(900m, planet.Materials);
        Assert.Equal(975m, planet.Deuterium);
        Assert.Equal(
            TechnologyType.EnergySystems,
            player.QueuedTechnology);

        Assert.False(ResearchService.Complete(
            player,
            startedAt.AddSeconds(14)));

        Assert.True(ResearchService.Complete(
            player,
            startedAt.AddSeconds(15)));

        Assert.Equal(
            1,
            ResearchService.GetLevel(
                player,
                TechnologyType.EnergySystems));

        Assert.Null(player.QueuedTechnology);
    }

    [Fact]
    public void Start_RejectsMissingPrerequisites()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander"
        };

        var planet = CreatePlanet(startedAt);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ResearchService.Start(
                player,
                planet,
                TechnologyType.Propulsion,
                startedAt));

        Assert.StartsWith(
            "Missing prerequisites:",
            exception.Message);
    }

    private static Planet CreatePlanet(DateTime startedAt)
    {
        return new Planet
        {
            Materials = 1000m,
            Deuterium = 1000m,
            ResearchLaboratoryLevel = 1,
            ResourcesUpdatedAt = startedAt
        };
    }
}
