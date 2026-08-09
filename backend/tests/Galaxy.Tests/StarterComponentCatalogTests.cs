using Galaxy.Application.Components;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Tests;

public class StarterComponentCatalogTests
{
    [Fact]
    public void EveryRace_HasAllMandatoryComponentTypes()
    {
        var requiredTypes = new[]
        {
            ComponentType.Hull,
            ComponentType.Engine,
            ComponentType.Reactor,
            ComponentType.ControlSystem
        };

        foreach (var race in Enum.GetValues<RaceType>())
        {
            var components =
                StarterComponentCatalog.GetForRace(race);

            Assert.Equal(4, components.Count);

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
}
