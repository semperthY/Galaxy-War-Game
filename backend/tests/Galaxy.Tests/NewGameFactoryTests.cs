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

        Assert.Equal("Commander", game.Player.Username);
        Assert.Single(game.Player.Planets);

        Assert.Equal("Homeworld", game.Homeworld.Name);
        Assert.Equal(500, game.Homeworld.Metal);
        Assert.Equal(500, game.Homeworld.Crystal);
        Assert.Equal(0, game.Homeworld.Deuterium);
        Assert.Equal(game.Player.Id, game.Homeworld.PlayerId);
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
