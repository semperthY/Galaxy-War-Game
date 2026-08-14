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
        Complete(player, utcNow);
        ResourceProductionCalculator.Update(planet, utcNow);

        var definition = TechnologyCatalog.Get(technology);
        var targetLevel = GetLevel(player, technology) + 1;

        if (!definition.Levels.TryGetValue(targetLevel, out var level))
        {
            throw new InvalidOperationException(
                "The maximum level available in Beta 2 has been reached.");
        }

        if (player.ResearchOrders.Any(x => x.Technology == technology))
        {
            throw new InvalidOperationException(
                "This technology is already being researched.");
        }

        if (planet.ResearchLaboratoryLevel < level.RequiredLaboratoryLevel)
        {
            throw new InvalidOperationException(
                $"Research laboratory level {level.RequiredLaboratoryLevel} is required.");
        }

        ValidatePrerequisites(player, level.Requirements);

        var availableStreams = GetAvailableStreamCount(player, planet);
        var occupiedStreams = planet.ResearchOrders
            .Select(x => x.StreamNumber)
            .ToHashSet();
        var streamNumber = Enumerable.Range(1, availableStreams)
            .FirstOrDefault(x => !occupiedStreams.Contains(x));

        if (streamNumber == 0)
        {
            throw new InvalidOperationException(
                "All available research streams on this planet are occupied.");
        }

        if (planet.Materials < level.Cost.Materials ||
            planet.Deuterium < level.Cost.Deuterium)
        {
            throw new InvalidOperationException("Not enough resources.");
        }

        planet.Materials -= level.Cost.Materials;
        planet.Deuterium -= level.Cost.Deuterium;

        var order = new ResearchOrder
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Player = player,
            PlanetId = planet.Id,
            Planet = planet,
            StreamNumber = streamNumber,
            Technology = technology,
            TargetLevel = targetLevel,
            StartedAt = utcNow,
            CompletesAt = utcNow.Add(level.Duration)
        };

        player.ResearchOrders.Add(order);
        if (!planet.ResearchOrders.Contains(order))
        {
            planet.ResearchOrders.Add(order);
        }

        return new ResearchResult(
            technology,
            targetLevel,
            streamNumber,
            level.Cost,
            order.CompletesAt,
            order);
    }

    public static IReadOnlyCollection<ResearchOrder> Complete(
        Player player,
        DateTime utcNow)
    {
        var completed = player.ResearchOrders
            .Where(x => x.CompletesAt <= utcNow)
            .ToList();

        foreach (var order in completed)
        {
            var technology = player.Technologies
                .SingleOrDefault(x => x.Technology == order.Technology);

            if (technology is null)
            {
                technology = new PlayerTechnology
                {
                    Id = Guid.NewGuid(),
                    PlayerId = player.Id,
                    Player = player,
                    Technology = order.Technology
                };
                player.Technologies.Add(technology);
            }

            technology.Level = Math.Max(technology.Level, order.TargetLevel);
            player.ResearchOrders.Remove(order);
            order.Planet.ResearchOrders.Remove(order);
        }

        return completed;
    }

    public static int GetLevel(Player player, TechnologyType technology) =>
        player.Technologies
            .SingleOrDefault(x => x.Technology == technology)
            ?.Level ?? 0;

    public static int GetAvailableStreamCount(Player player, Planet planet)
    {
        var coordinationLevel = GetLevel(
            player,
            TechnologyType.ResearchCoordination);

        if (coordinationLevel >= 2 && planet.ResearchLaboratoryLevel >= 9)
        {
            return 3;
        }

        if (coordinationLevel >= 1 && planet.ResearchLaboratoryLevel >= 5)
        {
            return 2;
        }

        return planet.ResearchLaboratoryLevel >= 1 ? 1 : 0;
    }

    public static ResearchCost CalculateCost(
        TechnologyType technology,
        int targetLevel) =>
        TechnologyCatalog.Get(technology).Levels.TryGetValue(
            targetLevel,
            out var level)
            ? level.Cost
            : throw new ArgumentOutOfRangeException(nameof(targetLevel));

    private static void ValidatePrerequisites(
        Player player,
        IReadOnlyCollection<TechnologyRequirement> requirements)
    {
        var missing = requirements
            .Where(x => GetLevel(player, x.Technology) < x.Level)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var required = string.Join(
            ", ",
            missing.Select(x =>
                $"{TechnologyCatalog.Get(x.Technology).Name} level {x.Level}"));

        throw new InvalidOperationException(
            $"Missing prerequisites: {required}.");
    }
}

public sealed record ResearchCost(decimal Materials, decimal Deuterium);

public sealed record ResearchResult(
    TechnologyType Technology,
    int TargetLevel,
    int StreamNumber,
    ResearchCost Cost,
    DateTime CompletesAt,
    ResearchOrder Order);

public sealed record TechnologyRequirement(
    TechnologyType Technology,
    int Level);
