using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Tests;

public class ShipDesignCalculatorTests
{
    [Fact]
    public void Calculate_CreatesValidMixedRaceDesign()
    {
        var result = ShipDesignCalculator.Calculate(
            "humans-hull-1",
            new[]
            {
                new ModuleSelection(
                    "synthetics-engine-1", 1),
                new ModuleSelection(
                    "energyforms-reactor-1", 1),
                new ModuleSelection(
                    "insectoids-control-1", 1)
            });

        Assert.Equal(50m, result.HullCapacity);
        Assert.Equal(28m, result.UsedVolume);
        Assert.Equal(22m, result.FreeVolume);
        Assert.Equal(70m, result.EnergyProduction);
        Assert.Equal(18m, result.EnergyConsumption);
    }

    [Fact]
    public void Calculate_CreatesColonizationDesign()
    {
        var result = ShipDesignCalculator.Calculate(
            "humans-hull-1",
            new[]
            {
                new ModuleSelection(
                    "humans-engine-1", 1),
                new ModuleSelection(
                    "humans-reactor-1", 1),
                new ModuleSelection(
                    "humans-control-1", 1),
                new ModuleSelection(
                    "humans-colony-1", 1)
            });

        Assert.Equal(45m, result.UsedVolume);
        Assert.Equal(5m, result.FreeVolume);

        Assert.Contains(
            result.RequiredComponents,
            x =>
                x.ComponentCode == "humans-colony-1" &&
                x.Quantity == 1);
    }

    [Fact]
    public void Calculate_AggregatesBeta2ShipStatistics()
    {
        var result = ShipDesignCalculator.Calculate(
            "test-hull",
            new[]
            {
                new ModuleSelection("test-engine", 1),
                new ModuleSelection("test-reactor", 1),
                new ModuleSelection("test-control", 1),
                new ModuleSelection("test-armor", 1),
                new ModuleSelection("test-shield", 1),
                new ModuleSelection("test-scanner", 1),
                new ModuleSelection("test-mining", 1),
                new ModuleSelection("test-cargo", 1),
                new ModuleSelection("test-laser", 1),
                new ModuleSelection("test-missile", 1)
            },
            CreateBeta2Catalog());

        Assert.Equal(65m, result.UsedVolume);
        Assert.Equal(55m, result.FreeVolume);
        Assert.Equal(160m, result.StructuralIntegrity);
        Assert.Equal(50m, result.ShieldCapacity);
        Assert.Equal(100m, result.EnergyProduction);
        Assert.Equal(40m, result.EnergyConsumption);
        Assert.Equal(60m, result.FreeEnergy);
        Assert.Equal(20m, result.CommandRating);
        Assert.Equal(13m, result.CommandLoad);
        Assert.Equal(7m, result.FreeCommandRating);
        Assert.Equal(120m, result.InSystemSpeed);
        Assert.Equal(25m, result.InterSystemSpeed);
        Assert.Equal(30m, result.ScanRange);
        Assert.Equal(100m, result.CargoCapacity);
        Assert.Equal(5m, result.MiningRatePerMinute);
        Assert.Equal(13m, result.ShieldDamage);
        Assert.Equal(14m, result.HullDamage);
    }

    [Fact]
    public void Calculate_RejectsExceededCommandCapacity()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ShipDesignCalculator.Calculate(
                "test-hull",
                new[]
                {
                    new ModuleSelection("test-engine", 1),
                    new ModuleSelection("test-reactor", 1),
                    new ModuleSelection("test-control", 1),
                    new ModuleSelection("test-scanner", 1),
                    new ModuleSelection("test-laser", 4),
                    new ModuleSelection("test-missile", 1)
                },
                CreateBeta2Catalog()));

        Assert.Equal(
            "Installed active systems exceed control system command capacity.",
            exception.Message);
    }


    [Fact]
    public void Calculate_RejectsMissingMandatorySystem()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ShipDesignCalculator.Calculate(
                "humans-hull-1",
                new[]
                {
                    new ModuleSelection(
                        "humans-engine-1", 1),
                    new ModuleSelection(
                        "humans-reactor-1", 1)
                }));

        Assert.Equal(
            "A control system is required.",
            exception.Message);
    }

    [Fact]
    public void Calculate_RejectsExceededHullCapacity()
    {
        Assert.Throws<InvalidOperationException>(
            () => ShipDesignCalculator.Calculate(
                "humans-hull-1",
                new[]
                {
                    new ModuleSelection(
                        "humans-engine-1", 3),
                    new ModuleSelection(
                        "humans-reactor-1", 1),
                    new ModuleSelection(
                        "humans-control-1", 1)
                }));
    }
    private static IReadOnlyCollection<IComponentDefinition>
        CreateBeta2Catalog()
    {
        var cost = new ComponentCost(1m, 1m);

        return new IComponentDefinition[]
        {
            new HullDefinition(
                "test-hull", "Test Hull", RaceType.Humans,
                cost, 1, TechnologyType.ShipEngineering, 1,
                120m, 100m),
            new EngineDefinition(
                "test-engine", "Test Engine", RaceType.Humans,
                8m, cost, 1, TechnologyType.EngineSystems, 1,
                120m, 25m, 4m),
            new ReactorDefinition(
                "test-reactor", "Test Reactor", RaceType.Humans,
                10m, cost, 1, TechnologyType.ReactorSystems, 1,
                100m),
            new ControlSystemDefinition(
                "test-control", "Test Control", RaceType.Humans,
                5m, cost, 1, TechnologyType.Electronics, 1,
                20m, 4m),
            new ArmorDefinition(
                "test-armor", "Test Armor", RaceType.Humans,
                7m, cost, 1, TechnologyType.ShipEngineering, 1,
                60m),
            new ShieldDefinition(
                "test-shield", "Test Shield", RaceType.Humans,
                7m, cost, 1, TechnologyType.FieldDefense, 1,
                50m, 12m),
            new ScannerDefinition(
                "test-scanner", "Test Scanner", RaceType.Humans,
                3m, cost, 1, TechnologyType.Electronics, 1,
                30m, 3m, 4m),
            new CargoHoldDefinition(
                "test-cargo", "Test Cargo", RaceType.Humans,
                10m, cost, 1, TechnologyType.IndustrialSystems, 1,
                100m, 0m),
            new MiningModuleDefinition(
                "test-mining", "Test Mining", RaceType.Humans,
                8m, cost, 1, TechnologyType.IndustrialSystems, 1,
                5m, 10m),
            new LaserWeaponDefinition(
                "test-laser", "Test Laser", RaceType.Humans,
                3m, cost, 1, TechnologyType.LaserSystems, 1,
                8m, 2m, 5m, 4m),
            new MissileWeaponDefinition(
                "test-missile", "Test Missile", RaceType.Humans,
                4m, cost, 1, TechnologyType.MissileSystems, 1,
                5m, 12m, 2m, 5m)
        };
    }

}
