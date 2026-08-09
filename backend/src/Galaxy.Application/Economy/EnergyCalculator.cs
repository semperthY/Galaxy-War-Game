using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class EnergyCalculator
{
    public const decimal PowerPlantBaseProduction = 25m;
    public const decimal MaterialsExtractorBaseConsumption = 6m;
    public const decimal DeuteriumExtractorBaseConsumption = 10m;
    public const decimal EnergyGrowthFactor = 1.10m;

    public static EnergyStatus Calculate(Planet planet)
    {
        var production = CalculateScaledValue(
            PowerPlantBaseProduction,
            planet.PowerPlantLevel);

        var consumption =
            CalculateScaledValue(
                MaterialsExtractorBaseConsumption,
                planet.MaterialsExtractorLevel) +
            CalculateScaledValue(
                DeuteriumExtractorBaseConsumption,
                planet.DeuteriumExtractorLevel);

        var efficiency = consumption == 0m
            ? 1m
            : decimal.Min(1m, production / consumption);

        return new EnergyStatus(
            decimal.Round(production, 4),
            decimal.Round(consumption, 4),
            decimal.Round(efficiency, 4));
    }

    private static decimal CalculateScaledValue(
        decimal baseValue,
        int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        return baseValue *
               level *
               Pow(EnergyGrowthFactor, level - 1);
    }

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;

        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }
}

public sealed record EnergyStatus(
    decimal Production,
    decimal Consumption,
    decimal Efficiency);
