using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class EnergyCalculator
{
    private const decimal EnergyPerPowerPlantLevel = 20m;
    private const decimal MaterialsExtractorConsumption = 5m;
    private const decimal DeuteriumExtractorConsumption = 10m;

    public static EnergyStatus Calculate(Planet planet)
    {
        var production =
            planet.PowerPlantLevel *
            EnergyPerPowerPlantLevel;

        var consumption =
            planet.MaterialsExtractorLevel *
            MaterialsExtractorConsumption +
            planet.DeuteriumExtractorLevel *
            DeuteriumExtractorConsumption;

        var efficiency = consumption == 0m
            ? 1m
            : decimal.Min(1m, production / consumption);

        return new EnergyStatus(
            production,
            consumption,
            efficiency);
    }
}

public sealed record EnergyStatus(
    decimal Production,
    decimal Consumption,
    decimal Efficiency);
