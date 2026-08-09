using Galaxy.Domain.Entities;

namespace Galaxy.Application.Games;

public static class NewGameFactory
{
    private const int SystemsPerGalaxy = 10;
    private const int PlanetsPerSystem = 8;

    public static NewGame Create(string username, RaceType race)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 32)
        {
            throw new ArgumentException(
                "Username must contain from 3 to 32 characters.",
                nameof(username));
        }

        var createdAt = DateTime.UtcNow;

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            CreatedAt = createdAt,
            Race = race
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
                var isHomeworld =
                    systemNumber == 1 &&
                    position == 1;

                var planet = new Planet
                {
                    Id = Guid.NewGuid(),
                    Name = isHomeworld
                        ? "Homeworld"
                        : $"Planet {systemNumber}:{position}",
                    Position = position,
                    Materials = isHomeworld ? 500m : 0m,
                    Deuterium = isHomeworld ? 100m : 0m,
                    MaterialsExtractorLevel = isHomeworld ? 1 : 0,
                    DeuteriumExtractorLevel = 0,
                    PowerPlantLevel = isHomeworld ? 1 : 0,
                    WarehouseLevel = isHomeworld ? 1 : 0,
                    ResearchLaboratoryLevel = 0,
                    ProductionComplexLevel = 0,
                    BuildingSiteCapacity = isHomeworld
                        ? 20
                        : 15 + ((systemNumber * 7 + position * 3) % 11),
                    ResourcesUpdatedAt = createdAt,
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




