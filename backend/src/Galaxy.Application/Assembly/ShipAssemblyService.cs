using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;

namespace Galaxy.Application.Assembly;

public static class ShipAssemblyService
{
    public static ShipAssemblyOrderResult Enqueue(
        Player player,
        Planet planet,
        ShipBlueprint blueprint,
        int quantity,
        DateTime utcNow)
    {
        Process(planet, utcNow);

        if (planet.AssemblyComplexLevel < 1)
        {
            throw new InvalidOperationException(
                "Assembly complex is required.");
        }

        if (blueprint.PlayerId != player.Id)
        {
            throw new InvalidOperationException(
                "Blueprint does not belong to the player.");
        }

        if (quantity is < 1 or > 20)
        {
            throw new InvalidOperationException(
                "Quantity must be between 1 and 20.");
        }

        var requirements = GetRequirements(
            blueprint,
            quantity);

        foreach (var requirement in requirements)
        {
            var available = planet.ComponentInventory
                .SingleOrDefault(x =>
                    string.Equals(
                        x.ComponentCode,
                        requirement.Key,
                        StringComparison.OrdinalIgnoreCase))
                ?.Quantity ?? 0;

            if (available < requirement.Value)
            {
                throw new InvalidOperationException(
                    $"Not enough component '{requirement.Key}'.");
            }
        }

        foreach (var requirement in requirements)
        {
            var item = planet.ComponentInventory
                .Single(x =>
                    string.Equals(
                        x.ComponentCode,
                        requirement.Key,
                        StringComparison.OrdinalIgnoreCase));

            item.Quantity -= requirement.Value;
        }

        var queuePosition = planet.AssemblyOrders.Count == 0
            ? 1
            : planet.AssemblyOrders.Max(x => x.QueuePosition) + 1;

        var order = new ShipAssemblyOrder
        {
            PlanetId = planet.Id,
            Planet = planet,
            ShipBlueprintId = blueprint.Id,
            Blueprint = blueprint,
            QueuePosition = queuePosition,
            Quantity = quantity
        };

        if (planet.AssemblyOrders.Count == 0)
        {
            StartOrder(
                order,
                planet.AssemblyComplexLevel,
                utcNow);
        }

        planet.AssemblyOrders.Add(order);

        return new ShipAssemblyOrderResult(
            order.QueuePosition,
            order.ShipBlueprintId,
            order.Quantity,
            order.StartedAt,
            order.CompletesAt,
            requirements);
    }

    public static int Process(
        Planet planet,
        DateTime utcNow)
    {
        var completedOrders = 0;

        while (true)
        {
            var activeOrder = planet.AssemblyOrders
                .OrderBy(x => x.QueuePosition)
                .FirstOrDefault();

            if (activeOrder is null)
            {
                break;
            }

            if (activeOrder.StartedAt is null)
            {
                StartOrder(
                    activeOrder,
                    planet.AssemblyComplexLevel,
                    utcNow);
            }

            if (activeOrder.CompletesAt > utcNow)
            {
                break;
            }

            var completionTime =
                activeOrder.CompletesAt!.Value;

            for (var index = 0;
                 index < activeOrder.Quantity;
                 index++)
            {
                planet.Ships.Add(new Ship
                {
                    PlayerId = activeOrder.Blueprint.PlayerId,
                    Player = activeOrder.Blueprint.Player,
                    PlanetId = planet.Id,
                    Planet = planet,
                    ShipBlueprintId = activeOrder.ShipBlueprintId,
                    Blueprint = activeOrder.Blueprint,
                    Name =
                        $"{activeOrder.Blueprint.Name} " +
                        $"Mk.{activeOrder.Blueprint.Version}",
                    CreatedAt = completionTime
                });
            }

            planet.AssemblyOrders.Remove(activeOrder);
            completedOrders++;

            var nextOrder = planet.AssemblyOrders
                .OrderBy(x => x.QueuePosition)
                .FirstOrDefault();

            if (nextOrder is not null)
            {
                StartOrder(
                    nextOrder,
                    planet.AssemblyComplexLevel,
                    completionTime);
            }
        }

        return completedOrders;
    }

    private static Dictionary<string, int> GetRequirements(
        ShipBlueprint blueprint,
        int quantity)
    {
        var modules = blueprint.Modules
            .Select(x => new ModuleSelection(
                x.ComponentCode,
                x.Quantity))
            .ToList();

        var design = ShipDesignCalculator.Calculate(
            blueprint.HullCode,
            modules);

        return design.RequiredComponents
            .GroupBy(
                x => x.ComponentCode,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(x => x.Quantity) * quantity,
                StringComparer.OrdinalIgnoreCase);
    }

    private static void StartOrder(
        ShipAssemblyOrder order,
        int complexLevel,
        DateTime startedAt)
    {
        var modules = order.Blueprint.Modules
            .Select(x => new ModuleSelection(
                x.ComponentCode,
                x.Quantity))
            .ToList();

        var design = ShipDesignCalculator.Calculate(
            order.Blueprint.HullCode,
            modules);

        var speedMultiplier =
            1m + (complexLevel - 1) * 0.1m;

        var durationSeconds = decimal.Ceiling(
            (30m + design.UsedVolume) *
            order.Quantity /
            speedMultiplier);

        order.StartedAt = startedAt;
        order.CompletesAt = startedAt.AddSeconds(
            (double)durationSeconds);
    }
}

public sealed record ShipAssemblyOrderResult(
    int QueuePosition,
    Guid BlueprintId,
    int Quantity,
    DateTime? StartedAt,
    DateTime? CompletesAt,
    IReadOnlyDictionary<string, int> RequiredComponents);

