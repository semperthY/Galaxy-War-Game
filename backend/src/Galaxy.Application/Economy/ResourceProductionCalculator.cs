using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class ResourceProductionCalculator
{
    private const decimal MaterialsPerHourPerLevel = 30m;
    private const decimal DeuteriumPerHourPerLevel = 10m;

    public static void Update(Planet planet, DateTime utcNow)
    {
        if (utcNow <= planet.ResourcesUpdatedAt)
        {
            return;
        }

        var elapsedSeconds =
            (decimal)(utcNow - planet.ResourcesUpdatedAt).TotalSeconds;

        var elapsedHours = elapsedSeconds / 3600m;
        var energy = EnergyCalculator.Calculate(planet);
        var storage = StorageCalculator.Calculate(planet);

        var producedMaterials =
            MaterialsPerHourPerLevel *
            planet.MaterialsExtractorLevel *
            elapsedHours *
            energy.Efficiency;

        var producedDeuterium =
            DeuteriumPerHourPerLevel *
            planet.DeuteriumExtractorLevel *
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
}
