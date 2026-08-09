using Galaxy.Application.Games;

namespace Galaxy.Tests;

public class NewGameFactoryTests
{
    [Fact]
    public void Create_GeneratesGalaxyAndHomeworld()
    {
        var game = NewGameFactory.Create("Commander");

        Assert.Equal(10, game.StarSystems.Count);
        Assert.Equal(
            80,
            game.StarSystems.Sum(system => system.Planets.Count));

        Assert.Single(game.Player.Planets);
        Assert.Equal("Homeworld", game.Homeworld.Name);
        Assert.Equal(500m, game.Homeworld.Materials);
        Assert.Equal(100m, game.Homeworld.Deuterium);
        Assert.Equal(1, game.Homeworld.MaterialsExtractorLevel);
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
            () => NewGameFactory.Create(username));
    }
}

