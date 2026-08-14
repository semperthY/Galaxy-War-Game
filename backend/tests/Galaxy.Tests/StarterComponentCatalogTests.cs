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

            Assert.Equal(30, components.Count);

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
    public void UniversalCatalog_MatchesBeta2MinimumSlice()
    {
        var universalComponents = StarterComponentCatalog.GetAll()
            .Where(x => x.Race is null)
            .ToList();

        Assert.Equal(25, universalComponents.Count);

        var expectedCodes = new[]
        {
            "HUL-01", "HUL-02",
            "ENG-01", "ENG-02", "ENG-03",
            "RCT-01", "RCT-02",
            "CTL-01", "CTL-02",
            "SNS-01", "SNS-02",
            "IND-01", "IND-05", "IND-06", "IND-08",
            "ARM-01", "ARM-02",
            "SHD-01", "SHD-02",
            "LAS-01", "LAS-02", "LAS-03",
            "MSL-01", "MSL-02", "MSL-03"
        };

        Assert.Equal(
            expectedCodes,
            universalComponents.Select(x => x.Code));

        var pioneer = Assert.IsType<ColonyModuleDefinition>(
            StarterComponentCatalog.Find("IND-08"));

        Assert.Equal(8m, pioneer.EnergyConsumption);
    }

    [Fact]
    public void ColonyModules_HaveRaceTradeoffs()
    {
        var modules = StarterComponentCatalog.GetAll()
            .OfType<ColonyModuleDefinition>()
            .Where(x => x.Race is not null)
            .ToList();

        Assert.Equal(4, modules.Count);
        Assert.Equal(4, modules.Select(x => x.Volume).Distinct().Count());
        Assert.All(modules, x => Assert.True(x.Volume > 0));
    }
}
