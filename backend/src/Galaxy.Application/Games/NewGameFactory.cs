using Galaxy.Domain.Entities;

namespace Galaxy.Application.Games;

public static class NewGameFactory
{
    private const int SystemsPerGalaxy = 10;
    private const int PlanetsPerSystem = 8;

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

        var systems = new List<StarSystem>();
        Planet? homeworld = null;

        for (var systemNumber = 1;
             systemNumber <= SystemsPerGalaxy;
             systemNumber++)
        {
            var starSystem = new StarSystem
            {
                Id = Guid.NewGuid(),
                GalaxyNumber = 1,
                SystemNumber = systemNumber,
                Name = $"System {systemNumber}"
            };

            for (var position = 1;
                 position <= PlanetsPerSystem;
                 position++)
            {
                var isHomeworld = systemNumber == 1 && position == 1;

                var planet = new Planet
                {
                    Id = Guid.NewGuid(),
                    Name = isHomeworld
                        ? "Homeworld"
                        : $"Planet {systemNumber}:{position}",
                    Position = position,
                    Metal = isHomeworld ? 500 : 0,
                    Crystal = isHomeworld ? 500 : 0,
                    Deuterium = 0,
                    PlayerId = isHomeworld ? player.Id : null,
                    Player = isHomeworld ? player : null,
                    StarSystemId = starSystem.Id,
                    StarSystem = starSystem
                };

                starSystem.Planets.Add(planet);

                if (isHomeworld)
                {
                    player.Planets.Add(planet);
                    homeworld = planet;
                }
            }

            systems.Add(starSystem);
        }

        return new NewGame(
            player,
            systems,
            homeworld ?? throw new InvalidOperationException(
                "Homeworld was not generated."));
    }
}

public sealed record NewGame(
    Player Player,
    IReadOnlyCollection<StarSystem> StarSystems,
    Planet Homeworld);
