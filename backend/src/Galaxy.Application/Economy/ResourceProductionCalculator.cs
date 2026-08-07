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

        planet.Materials +=
            MaterialsPerHourPerLevel *
            planet.MaterialsExtractorLevel *
            elapsedHours *
            energy.Efficiency;

        planet.Deuterium +=
            DeuteriumPerHourPerLevel *
            planet.DeuteriumExtractorLevel *
            elapsedHours *
            energy.Efficiency;

        planet.Materials = decimal.Round(
            planet.Materials, 4, MidpointRounding.ToZero);

        planet.Deuterium = decimal.Round(
            planet.Deuterium, 4, MidpointRounding.ToZero);

        planet.ResourcesUpdatedAt = utcNow;
    }
}
