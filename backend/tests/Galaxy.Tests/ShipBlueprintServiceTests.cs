using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class ShipBlueprintServiceTests
{
    [Fact]
    public void Create_CreatesNextBlueprintVersion()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Commander"
        };

        var modules = new[]
        {
            new ModuleSelection("humans-engine-1", 1),
            new ModuleSelection("humans-reactor-1", 1),
            new ModuleSelection("humans-control-1", 1)
        };

        var first = ShipBlueprintService.Create(
            player,
            "Hornet",
            "humans-hull-1",
            modules,
            DateTime.UtcNow);

        var second = ShipBlueprintService.Create(
            player,
            "Hornet",
            "humans-hull-1",
            modules,
            DateTime.UtcNow);

        Assert.Equal(1, first.Blueprint.Version);
        Assert.Equal(2, second.Blueprint.Version);
        Assert.Equal(2, player.Blueprints.Count);
    }
}
