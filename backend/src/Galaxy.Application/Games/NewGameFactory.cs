using Galaxy.Domain.Entities;

namespace Galaxy.Application.Games;

public static class NewGameFactory
{
    public static NewGame Create(string username)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 32)
        {
            throw new ArgumentException(
                "Username must contain from 3 to 32 characters.",
                nameof(username));
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            CreatedAt = DateTime.UtcNow
        };

        var starSystem = new StarSystem
        {
            Id = Guid.NewGuid(),
            GalaxyNumber = 1,
            SystemNumber = 1,
            Name = "System 1"
        };

        var planet = new Planet
        {
            Id = Guid.NewGuid(),
            Name = "Homeworld",
            Position = 1,
            Metal = 500,
            Crystal = 500,
            Deuterium = 0,
            PlayerId = player.Id,
            Player = player,
            StarSystemId = starSystem.Id,
            StarSystem = starSystem
        };

        player.Planets.Add(planet);
        starSystem.Planets.Add(planet);

        return new NewGame(player, starSystem, planet);
    }
}

public sealed record NewGame(
    Player Player,
    StarSystem StarSystem,
    Planet Planet);
