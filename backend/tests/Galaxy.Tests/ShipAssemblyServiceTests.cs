using Galaxy.Application.Assembly;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ShipAssemblyServiceTests
{
    [Fact]
    public void EnqueueAndProcess_PutsShipsInReserve()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander",
            Race = RaceType.Humans
        };

        var blueprint = CreateBlueprint(player);

        var planet = new Planet
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            AssemblyComplexLevel = 1
        };

        AddInventory(
            planet,
            "humans-hull-1",
            2);

        AddInventory(
            planet,
            "humans-engine-1",
            2);

        AddInventory(
            planet,
            "humans-reactor-1",
            2);

        AddInventory(
            planet,
            "humans-control-1",
            2);

        var result = ShipAssemblyService.Enqueue(
            player,
            planet,
            blueprint,
            2,
            startedAt);

        Assert.All(
            planet.ComponentInventory,
            item => Assert.Equal(0, item.Quantity));

        Assert.Equal(
            0,
            ShipAssemblyService.Process(
                planet,
                result.CompletesAt!.Value.AddSeconds(-1)));

        Assert.Equal(
            1,
            ShipAssemblyService.Process(
                planet,
                result.CompletesAt.Value));

        Assert.Empty(planet.AssemblyOrders);
        Assert.Equal(2, planet.Ships.Count);

        Assert.All(
            planet.Ships,
            ship =>
            {
                Assert.Equal(player.Id, ship.PlayerId);
                Assert.Equal(blueprint.Id, ship.ShipBlueprintId);
                Assert.Equal("Hornet Mk.1", ship.Name);
            });
    }

    [Fact]
    public void Enqueue_RejectsMissingComponents()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander",
            Race = RaceType.Humans
        };

        var blueprint = CreateBlueprint(player);

        var planet = new Planet
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            AssemblyComplexLevel = 1
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => ShipAssemblyService.Enqueue(
                player,
                planet,
                blueprint,
                1,
                DateTime.UtcNow));

        Assert.Contains(
            "Not enough component",
            exception.Message);
    }

    private static ShipBlueprint CreateBlueprint(
        Player player)
    {
        var blueprint = new ShipBlueprint
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            Name = "Hornet",
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

        player.Blueprints.Add(blueprint);

        return blueprint;
    }

    private static void AddInventory(
        Planet planet,
        string componentCode,
        int quantity)
    {
        planet.ComponentInventory.Add(
            new ComponentInventoryItem
            {
                PlanetId = planet.Id,
                Planet = planet,
                ComponentCode = componentCode,
                Quantity = quantity
            });
    }
}
