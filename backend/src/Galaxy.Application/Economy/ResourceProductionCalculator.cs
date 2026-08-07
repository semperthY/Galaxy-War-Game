using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class ResourceProductionCalculator
{
    private const decimal MetalPerHourPerLevel = 30m;
    private const decimal CrystalPerHourPerLevel = 20m;
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

        planet.Metal +=
            MetalPerHourPerLevel *
            planet.MetalMineLevel *
            elapsedHours;

        planet.Crystal +=
            CrystalPerHourPerLevel *
            planet.CrystalMineLevel *
            elapsedHours;

        planet.Deuterium +=
            DeuteriumPerHourPerLevel *
            planet.DeuteriumMineLevel *
            elapsedHours;

        planet.Metal = decimal.Round(
            planet.Metal, 4, MidpointRounding.ToZero);

        planet.Crystal = decimal.Round(
            planet.Crystal, 4, MidpointRounding.ToZero);

        planet.Deuterium = decimal.Round(
            planet.Deuterium, 4, MidpointRounding.ToZero);

        planet.ResourcesUpdatedAt = utcNow;
    }
}

