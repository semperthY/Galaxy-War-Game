using Galaxy.Application.Economy;
using Galaxy.Domain.Entities;

namespace Galaxy.Application.Research;

public static class ResearchService
{
    public static ResearchResult Start(
        Player player,
        Planet planet,
        TechnologyType technology,
        DateTime utcNow)
    {
        ResourceProductionCalculator.Update(planet, utcNow);

        if (planet.ResearchLaboratoryLevel < 1)
        {
            throw new InvalidOperationException(
                "A research laboratory is required.");
        }

        if (player.QueuedTechnology is not null)
        {
            throw new InvalidOperationException(
                "Research is already in progress.");
        }

        var targetLevel =
            GetLevel(player, technology) + 1;

        if (targetLevel > planet.ResearchLaboratoryLevel)
        {
            throw new InvalidOperationException(
                "Research laboratory level is too low.");
        }

        ValidatePrerequisites(player, technology);

        var cost = CalculateCost(
            technology,
            targetLevel);

        if (planet.Materials < cost.Materials ||
            planet.Deuterium < cost.Deuterium)
        {
            throw new InvalidOperationException(
                "Not enough resources.");
        }

        planet.Materials -= cost.Materials;
        planet.Deuterium -= cost.Deuterium;

        var durationSeconds = Math.Max(
            5,
            targetLevel * 15 /
            planet.ResearchLaboratoryLevel);

        player.QueuedTechnology = technology;
        player.QueuedTechnologyLevel = targetLevel;
        player.ResearchCompletesAt =
            utcNow.AddSeconds(durationSeconds);

        return new ResearchResult(
            technology,
            targetLevel,
            cost,
            player.ResearchCompletesAt.Value);
    }

    public static bool Complete(
        Player player,
        DateTime utcNow)
    {
        if (player.QueuedTechnology is null ||
            player.QueuedTechnologyLevel is null ||
            player.ResearchCompletesAt is null ||
            utcNow < player.ResearchCompletesAt.Value)
        {
            return false;
        }

        var technologyType =
            player.QueuedTechnology.Value;

        var technology = player.Technologies
            .SingleOrDefault(x =>
                x.Technology == technologyType);

        if (technology is null)
        {
            technology = new PlayerTechnology
            {
                PlayerId = player.Id,
                Player = player,
                Technology = technologyType
            };

            player.Technologies.Add(technology);
        }

        technology.Level =
            player.QueuedTechnologyLevel.Value;

        player.QueuedTechnology = null;
        player.QueuedTechnologyLevel = null;
        player.ResearchCompletesAt = null;

        return true;
    }

    public static int GetLevel(
        Player player,
        TechnologyType technology)
    {
        return player.Technologies
            .SingleOrDefault(x =>
                x.Technology == technology)
            ?.Level ?? 0;
    }

    public static ResearchCost CalculateCost(
        TechnologyType technology,
        int targetLevel)
    {
        if (targetLevel < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetLevel));
        }

        var multiplier = Pow(
            1.6m,
            targetLevel - 1);

        var baseCost = technology switch
        {
            TechnologyType.MaterialsScience =>
                new ResearchCost(100m, 0m),

            TechnologyType.EnergySystems =>
                new ResearchCost(100m, 25m),

            TechnologyType.DeuteriumTechnology =>
                new ResearchCost(120m, 40m),

            TechnologyType.ControlSystems =>
                new ResearchCost(140m, 40m),

            TechnologyType.Propulsion =>
                new ResearchCost(180m, 60m),

            TechnologyType.ComponentEngineering =>
                new ResearchCost(200m, 75m),

            _ => throw new ArgumentOutOfRangeException(
                nameof(technology))
        };

        return new ResearchCost(
            decimal.Ceiling(
                baseCost.Materials * multiplier),
            decimal.Ceiling(
                baseCost.Deuterium * multiplier));
    }

    private static void ValidatePrerequisites(
        Player player,
        TechnologyType technology)
    {
        var prerequisites = technology switch
        {
            TechnologyType.DeuteriumTechnology =>
                new[]
                {
                    new TechnologyRequirement(
                        TechnologyType.EnergySystems, 1)
                },

            TechnologyType.ControlSystems =>
                new[]
                {
                    new TechnologyRequirement(
                        TechnologyType.EnergySystems, 1)
                },

            TechnologyType.Propulsion =>
                new[]
                {
                    new TechnologyRequirement(
                        TechnologyType.MaterialsScience, 1),
                    new TechnologyRequirement(
                        TechnologyType.EnergySystems, 1)
                },

            TechnologyType.ComponentEngineering =>
                new[]
                {
                    new TechnologyRequirement(
                        TechnologyType.MaterialsScience, 1),
                    new TechnologyRequirement(
                        TechnologyType.ControlSystems, 1)
                },

            _ => Array.Empty<TechnologyRequirement>()
        };

        var missing = prerequisites
            .Where(requirement =>
                GetLevel(player, requirement.Technology) <
                requirement.Level)
            .ToList();

        if (missing.Count > 0)
        {
            var required = string.Join(
                ", ",
                missing.Select(x =>
                    $"{x.Technology} level {x.Level}"));

            throw new InvalidOperationException(
                $"Missing prerequisites: {required}.");
        }
    }

    private static decimal Pow(
        decimal value,
        int exponent)
    {
        var result = 1m;

        for (var index = 0; index < exponent; index++)
        {
            result *= value;
        }

        return result;
    }
}

public sealed record ResearchCost(
    decimal Materials,
    decimal Deuterium);

public sealed record ResearchResult(
    TechnologyType Technology,
    int TargetLevel,
    ResearchCost Cost,
    DateTime CompletesAt);

public sealed record TechnologyRequirement(
    TechnologyType Technology,
    int Level);

