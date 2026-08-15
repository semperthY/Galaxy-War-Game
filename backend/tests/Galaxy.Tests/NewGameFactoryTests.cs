using Galaxy.Application.Games;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class NewGameFactoryTests
{
    [Fact]
    public void Create_GeneratesGalaxyAndHomeworld()
    {
        var game = NewGameFactory.Create("Commander", RaceType.Humans);

        Assert.Equal(10, game.StarSystems.Count);
        Assert.Equal(
            80,
            game.StarSystems.Sum(system => system.Planets.Count));

        Assert.Single(game.Player.Planets);
        Assert.Equal("Homeworld", game.Homeworld.Name);
        Assert.Equal(1200m, game.Homeworld.Materials);
        Assert.Equal(400m, game.Homeworld.Deuterium);
        Assert.Equal(2, game.Homeworld.MaterialsExtractorLevel);
        Assert.Equal(1, game.Homeworld.DeuteriumExtractorLevel);
        Assert.Equal(1, game.Homeworld.PowerPlantLevel);
        Assert.Equal(1, game.Homeworld.WarehouseLevel);
        Assert.Equal(20, game.Homeworld.BuildingSiteCapacity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Create_RejectsInvalidUsername(string username)
    {
        Assert.Throws<ArgumentException>(
            () => NewGameFactory.Create(username, RaceType.Humans));
    }

    [Fact]
    public void Create_RejectsMissingRace()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => NewGameFactory.Create("Commander", (RaceType)0));

        Assert.Equal("race", exception.ParamName);
    }

    [Fact]
    public void ClaimHomeworld_CreatesIndependentStartingState()
    {
        var starSystem = new StarSystem
        {
            Id = Guid.NewGuid(),
            GalaxyNumber = 1,
            SystemNumber = 2,
            Name = "System 2"
        };
        var planet = new Planet
        {
            Id = Guid.NewGuid(),
            Name = "Planet 2:4",
            Position = 4,
            BuildingSiteCapacity = 17,
            StarSystemId = starSystem.Id,
            StarSystem = starSystem
        };

        var player = NewGameFactory.ClaimHomeworld(
            "SecondCommander",
            RaceType.Synthetics,
            planet,
            DateTime.UtcNow);

        Assert.Same(player, planet.Player);
        Assert.Equal(player.Id, planet.PlayerId);
        Assert.Single(player.Planets);
        Assert.Equal(1200m, planet.Materials);
        Assert.Equal(400m, planet.Deuterium);
        Assert.Equal(2, planet.MaterialsExtractorLevel);
        Assert.Equal(1, planet.DeuteriumExtractorLevel);
        Assert.Equal(0, planet.ResearchLaboratoryLevel);
        Assert.Equal(20, planet.BuildingSiteCapacity);
    }
}
