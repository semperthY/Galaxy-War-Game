using Galaxy.Application.ShipDesign;

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
}
