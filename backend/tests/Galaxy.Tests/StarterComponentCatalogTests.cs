using Galaxy.Application.Components;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Tests;

public class StarterComponentCatalogTests
{
    [Fact]
    public void EveryRace_HasRequiredComponentTypes()
    {
        var requiredTypes = new[]
        {
            ComponentType.Hull,
            ComponentType.Engine,
            ComponentType.Reactor,
            ComponentType.ControlSystem,
            ComponentType.ColonyModule
        };

        foreach (var race in Enum.GetValues<RaceType>())
        {
            var components =
                StarterComponentCatalog.GetForRace(race);

            Assert.Equal(5, components.Count);

            foreach (var requiredType in requiredTypes)
            {
                Assert.Contains(
                    components,
                    x => x.Type == requiredType);
            }
        }
    }

    [Fact]
    public void ComponentCodes_AreUnique()
    {
        var components = StarterComponentCatalog.GetAll();

        Assert.Equal(
            components.Count,
            components.Select(x => x.Code).Distinct().Count());
    }

    [Fact]
    public void ColonyModules_HaveRaceTradeoffs()
    {
        var modules = StarterComponentCatalog.GetAll()
            .OfType<ColonyModuleDefinition>()
            .ToList();

        Assert.Equal(4, modules.Count);
        Assert.Equal(4, modules.Select(x => x.Volume).Distinct().Count());
        Assert.All(modules, x => Assert.True(x.Volume > 0));
    }
}
