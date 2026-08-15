using Galaxy.Application.Games;
using Galaxy.Domain.Entities;

namespace Galaxy.Application.Economy;

public static class EarlyGameEconomySimulator
{
    private static readonly IReadOnlyList<EarlyGameAction> Actions =
        new[]
        {
            A(0, "Экстрактор материалов II → III", 135, 0),
            A(0.02, "Экстрактор дейтерия I → II", 120, 30),
            A(0.17, "Исследовательская лаборатория I", 150, 25),
            A(0.33, "Производственный комплекс I", 200, 50),
            A(1, "Материаловедение I", 300, 0),
            A(2, "Вычислительные системы I", 220, 40),
            A(3.5, "Энергетика I", 250, 80),
            A(5, "Корабельная инженерия I", 350, 20),
            A(7, "Двигательные системы I", 300, 100),
            A(10, "Реакторные системы I", 280, 100),
            A(14, "Электроника I", 250, 50),
            A(18, "Промышленные системы I", 300, 20),
            A(24, "Производственный комплекс II", 300, 75),
            A(32, "Лаборатория II", 225, 38),
            A(40, "Материаловедение II", 800, 40),
            A(50, "Энергетика II", 700, 280),
            A(62, "Корабельная инженерия II", 1000, 100),
            A(64, "Сборочный комплекс I", 300, 100)
        };

    public static EarlyGameSimulationResult Simulate()
    {
        var startedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var planet = new Planet
        {
            Materials = NewGameFactory.StartingMaterials,
            Deuterium = NewGameFactory.StartingDeuterium,
            MaterialsExtractorLevel = 2,
            DeuteriumExtractorLevel = 1,
            PowerPlantLevel = 1,
            WarehouseLevel = 1,
            ResourcesUpdatedAt = startedAt
        };

        var now = startedAt;
        var snapshots = new List<EarlyGameSnapshot>();
        var maxBlocked = TimeSpan.Zero;

        foreach (var action in Actions)
        {
            var plannedAt = startedAt.AddHours(action.PlannedHour);
            Advance(planet, ref now, plannedAt);

            var blockedSince = now;
            while (!CanAfford(planet, action) &&
                   now < startedAt.AddHours(72))
            {
                Advance(planet, ref now, now.AddMinutes(15));
            }

            if (!CanAfford(planet, action))
            {
                break;
            }

            maxBlocked = TimeSpan.FromTicks(Math.Max(
                maxBlocked.Ticks,
                (now - blockedSince).Ticks));

            planet.Materials -= action.Materials;
            planet.Deuterium -= action.Deuterium;
            snapshots.Add(new EarlyGameSnapshot(
                action.Name,
                now - startedAt,
                planet.Materials,
                planet.Deuterium,
                now - blockedSince));
        }

        Advance(planet, ref now, startedAt.AddHours(72));

        return new EarlyGameSimulationResult(
            snapshots,
            maxBlocked,
            planet.Materials,
            planet.Deuterium);
    }

    private static void Advance(
        Planet planet,
        ref DateTime now,
        DateTime target)
    {
        if (target <= now)
        {
            return;
        }

        ResourceProductionCalculator.Update(planet, target);
        now = target;
    }

    private static bool CanAfford(
        Planet planet,
        EarlyGameAction action) =>
        planet.Materials >= action.Materials &&
        planet.Deuterium >= action.Deuterium;

    private static EarlyGameAction A(
        double hour,
        string name,
        decimal materials,
        decimal deuterium) =>
        new(name, hour, materials, deuterium);
}

public sealed record EarlyGameAction(
    string Name,
    double PlannedHour,
    decimal Materials,
    decimal Deuterium);

public sealed record EarlyGameSnapshot(
    string Action,
    TimeSpan StartedAfter,
    decimal MaterialsAfter,
    decimal DeuteriumAfter,
    TimeSpan BlockedDuration);

public sealed record EarlyGameSimulationResult(
    IReadOnlyList<EarlyGameSnapshot> Actions,
    TimeSpan MaxBlockedDuration,
    decimal MaterialsAfter72Hours,
    decimal DeuteriumAfter72Hours);
