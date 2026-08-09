using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class ResourceProductionCalculator
{
    public const decimal MaterialsBaseHourlyRate = 40m;
    public const decimal DeuteriumBaseHourlyRate = 15m;
    public const decimal ProductionGrowthFactor = 1.12m;

    public static decimal CalculateMaterialsPerHour(int level)
    {
        return CalculateHourlyProduction(
            MaterialsBaseHourlyRate,
            level);
    }

    public static decimal CalculateDeuteriumPerHour(int level)
    {
        return CalculateHourlyProduction(
            DeuteriumBaseHourlyRate,
            level);
    }

    public static void Update(Planet planet, DateTime utcNow)
    {
        if (utcNow <= planet.ResourcesUpdatedAt)
        {
            return;
        }

        var elapsedHours =
            (decimal)(utcNow - planet.ResourcesUpdatedAt).TotalSeconds /
            3600m;

        var energy = EnergyCalculator.Calculate(planet);
        var storage = StorageCalculator.Calculate(planet);

        var producedMaterials =
            CalculateMaterialsPerHour(
                planet.MaterialsExtractorLevel) *
            elapsedHours *
            energy.Efficiency;

        var producedDeuterium =
            CalculateDeuteriumPerHour(
                planet.DeuteriumExtractorLevel) *
            elapsedHours *
            energy.Efficiency;

        planet.Materials = AddProducedResource(
            planet.Materials,
            producedMaterials,
            storage.Materials);

        planet.Deuterium = AddProducedResource(
            planet.Deuterium,
            producedDeuterium,
            storage.Deuterium);

        planet.ResourcesUpdatedAt = utcNow;
    }

    private static decimal CalculateHourlyProduction(
        decimal baseRate,
        int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        return decimal.Round(
            baseRate *
            level *
            Pow(ProductionGrowthFactor, level - 1),
            4,
            MidpointRounding.ToZero);
    }

    private static decimal AddProducedResource(
        decimal current,
        decimal produced,
        decimal capacity)
    {
        var availableSpace = decimal.Max(
            0m,
            capacity - current);

        var acceptedProduction = decimal.Min(
            produced,
            availableSpace);

        return decimal.Round(
            current + acceptedProduction,
            4,
            MidpointRounding.ToZero);
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
