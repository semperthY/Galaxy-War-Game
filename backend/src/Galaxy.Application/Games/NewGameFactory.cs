using Galaxy.Domain.Entities;

namespace Galaxy.Application.Games;

public static class NewGameFactory
{
    public const decimal StartingMaterials = 1200m;
    public const decimal StartingDeuterium = 400m;

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

        if (!Enum.IsDefined(race))
        {
            throw new ArgumentException(
                "Race must be selected.",
                nameof(race));
        }

        var createdAt = DateTime.UtcNow;

        var player = CreatePlayer(username, race, createdAt);

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
                    Materials = isHomeworld ? StartingMaterials : 0m,
                    Deuterium = isHomeworld ? StartingDeuterium : 0m,
                    MaterialsExtractorLevel = isHomeworld ? 2 : 0,
                    DeuteriumExtractorLevel = isHomeworld ? 1 : 0,
                    PowerPlantLevel = isHomeworld ? 1 : 0,
                    WarehouseLevel = isHomeworld ? 1 : 0,
                    ResearchLaboratoryLevel = 0,
                    ProductionComplexLevel = 0,
                    AssemblyComplexLevel = 0,
                    ShipyardLevel = 0,
                    RaceEngineeringComplexLevel = 0,
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

    public static Player ClaimHomeworld(
        string username,
        RaceType race,
        Planet planet,
        DateTime createdAt)
    {
        var player = CreatePlayer(username, race, createdAt);

        planet.Name = $"{username}'s Homeworld";
        planet.Materials = StartingMaterials;
        planet.Deuterium = StartingDeuterium;
        planet.MaterialsExtractorLevel = 2;
        planet.DeuteriumExtractorLevel = 1;
        planet.PowerPlantLevel = 1;
        planet.WarehouseLevel = 1;
        planet.ResearchLaboratoryLevel = 0;
        planet.ProductionComplexLevel = 0;
        planet.AssemblyComplexLevel = 0;
        planet.ShipyardLevel = 0;
        planet.RaceEngineeringComplexLevel = 0;
        planet.BuildingSiteCapacity = Math.Max(planet.BuildingSiteCapacity, 20);
        planet.ResourcesUpdatedAt = createdAt;
        planet.QueuedBuilding = null;
        planet.QueuedBuildingLevel = null;
        planet.BuildingCompletesAt = null;
        planet.PlayerId = player.Id;
        planet.Player = player;
        player.Planets.Add(planet);

        return player;
    }

    private static Player CreatePlayer(
        string username,
        RaceType race,
        DateTime createdAt)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 32)
        {
            throw new ArgumentException(
                "Username must contain from 3 to 32 characters.",
                nameof(username));
        }

        if (!Enum.IsDefined(race))
        {
            throw new ArgumentException(
                "Race must be selected.",
                nameof(race));
        }

        return new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            CreatedAt = createdAt,
            Race = race
        };
    }
}

public sealed record NewGame(
    Player Player,
    IReadOnlyCollection<StarSystem> StarSystems,
    Planet Homeworld);
