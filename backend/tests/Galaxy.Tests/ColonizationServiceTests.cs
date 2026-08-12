using Galaxy.Application.Colonization;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ColonizationServiceTests
{
    [Fact]
    public void Begin_CreatesTimedOperationWithoutMutatingTrackedCollections()
    {
        var utcNow = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var state = CreateState(includeColonyModule: true);

        var operation = ColonizationService.Begin(
            state.Player,
            state.Ship,
            state.Target,
            utcNow);

        Assert.Null(state.Target.PlayerId);
        Assert.Equal(state.Target.Id, operation.TargetPlanetId);
        Assert.Equal(state.Ship.Id, operation.ConsumedShipId);
        Assert.Equal(
            utcNow.Add(ColonizationService.DeploymentDuration),
            operation.CompletesAt);
        Assert.Empty(state.Player.ColonizationOperations);
        Assert.Contains(state.Ship, state.Origin.Ships);
        Assert.Contains(state.Ship, state.Player.Ships);
    }

    [Fact]
    public void Complete_ClaimsPlanetWithIndependentStartingState()
    {
        var utcNow = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var state = CreateState(includeColonyModule: true);
        var operation = ColonizationService.Begin(
            state.Player,
            state.Ship,
            state.Target,
            utcNow);

        var result = ColonizationService.Complete(
            operation,
            operation.CompletesAt);

        Assert.Same(state.Target, result);
        Assert.Equal(state.Player.Id, state.Target.PlayerId);
        Assert.Equal("Colony 1:2", state.Target.Name);
        Assert.Equal(250m, state.Target.Materials);
        Assert.Equal(50m, state.Target.Deuterium);
        Assert.Equal(1, state.Target.MaterialsExtractorLevel);
        Assert.Equal(1, state.Target.PowerPlantLevel);
        Assert.Equal(1, state.Target.WarehouseLevel);
        Assert.Contains(state.Target, state.Player.Planets);
        Assert.Equal(operation.CompletesAt, operation.CompletedAt);
    }

    [Fact]
    public void Complete_RejectsOperationBeforeDeadline()
    {
        var utcNow = DateTime.UtcNow;
        var state = CreateState(includeColonyModule: true);
        var operation = ColonizationService.Begin(
            state.Player,
            state.Ship,
            state.Target,
            utcNow);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ColonizationService.Complete(
                operation,
                operation.CompletesAt.AddSeconds(-1)));

        Assert.Equal(
            "Colonization deployment is not complete yet.",
            exception.Message);
        Assert.Null(state.Target.PlayerId);
    }

    [Fact]
    public void Colonize_RejectsShipWithoutColonyModule()
    {
        var state = CreateState(includeColonyModule: false);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ColonizationService.Begin(
                state.Player,
                state.Ship,
                state.Target,
                DateTime.UtcNow));

        Assert.Equal(
            "Ship does not have a colony module.",
            exception.Message);
    }

    [Fact]
    public void Colonize_RejectsPlanetInAnotherSystem()
    {
        var state = CreateState(includeColonyModule: true);

        state.Target.StarSystemId = Guid.NewGuid();

        var exception = Assert.Throws<InvalidOperationException>(
            () => ColonizationService.Begin(
                state.Player,
                state.Ship,
                state.Target,
                DateTime.UtcNow));

        Assert.Equal(
            "Colonization is currently limited to the same star system.",
            exception.Message);
    }

    private static ColonizationTestState CreateState(
        bool includeColonyModule)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander",
            Race = RaceType.Humans
        };

        var system = new StarSystem
        {
            Id = Guid.NewGuid(),
            GalaxyNumber = 1,
            SystemNumber = 1,
            Name = "System 1"
        };

        var origin = new Planet
        {
            Id = Guid.NewGuid(),
            Name = "Homeworld",
            Position = 1,
            PlayerId = player.Id,
            Player = player,
            StarSystemId = system.Id,
            StarSystem = system
        };

        var target = new Planet
        {
            Id = Guid.NewGuid(),
            Name = "Planet 1:2",
            Position = 2,
            BuildingSiteCapacity = 20,
            StarSystemId = system.Id,
            StarSystem = system
        };

        var blueprint = new ShipBlueprint
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            Name = "Pioneer",
            Version = 1,
            HullCode = "humans-hull-1"
        };

        blueprint.Modules.Add(new ShipBlueprintModule
        {
            ShipBlueprint = blueprint,
            ComponentCode = "humans-engine-1",
            Quantity = 1
        });

        blueprint.Modules.Add(new ShipBlueprintModule
        {
            ShipBlueprint = blueprint,
            ComponentCode = "humans-reactor-1",
            Quantity = 1
        });

        blueprint.Modules.Add(new ShipBlueprintModule
        {
            ShipBlueprint = blueprint,
            ComponentCode = "humans-control-1",
            Quantity = 1
        });

        if (includeColonyModule)
        {
            blueprint.Modules.Add(new ShipBlueprintModule
            {
                ShipBlueprint = blueprint,
                ComponentCode = "humans-colony-1",
                Quantity = 1
            });
        }

        var ship = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            PlanetId = origin.Id,
            Planet = origin,
            ShipBlueprintId = blueprint.Id,
            Blueprint = blueprint,
            Name = "Pioneer Mk.1"
        };

        player.Planets.Add(origin);
        player.Blueprints.Add(blueprint);
        player.Ships.Add(ship);
        origin.Ships.Add(ship);
        system.Planets.Add(origin);
        system.Planets.Add(target);

        return new ColonizationTestState(
            player,
            origin,
            target,
            ship);
    }

    private sealed record ColonizationTestState(
        Player Player,
        Planet Origin,
        Planet Target,
        Ship Ship);
}
