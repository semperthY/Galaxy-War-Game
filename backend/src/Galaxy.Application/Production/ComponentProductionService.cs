using Galaxy.Application.Components;
using Galaxy.Application.Economy;
using Galaxy.Application.Research;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.Production;

public static class ComponentProductionService
{
    public static ProductionOrderResult Enqueue(
        Player player,
        Planet planet,
        int lineNumber,
        string componentCode,
        int quantity,
        DateTime utcNow)
    {
        Process(planet, utcNow);
        ResourceProductionCalculator.Update(planet, utcNow);

        var lineCount = GetLineCount(planet);

        if (lineNumber < 1 || lineNumber > lineCount)
        {
            throw new InvalidOperationException(
                "Production line is not available.");
        }

        if (quantity is < 1 or > 100)
        {
            throw new InvalidOperationException(
                "Quantity must be between 1 and 100.");
        }

        var component = StarterComponentCatalog.Find(
            componentCode);

        if (component is null)
        {
            throw new InvalidOperationException(
                "Component does not exist.");
        }

        if (component.Race is not null &&
            component.Race != player.Race)
        {
            throw new InvalidOperationException(
                "This race cannot manufacture the component.");
        }

        if (ResearchService.GetLevel(
                player,
                component.RequiredTechnology) <
            component.RequiredTechnologyLevel)
        {
            throw new InvalidOperationException(
                $"Required technology: " +
                $"{component.RequiredTechnology} " +
                $"level {component.RequiredTechnologyLevel}.");
        }

        var materialsCost =
            component.Cost.Materials * quantity;

        var deuteriumCost =
            component.Cost.Deuterium * quantity;

        if (planet.Materials < materialsCost ||
            planet.Deuterium < deuteriumCost)
        {
            throw new InvalidOperationException(
                "Not enough resources.");
        }

        planet.Materials -= materialsCost;
        planet.Deuterium -= deuteriumCost;

        var lineOrders = planet.ProductionOrders
            .Where(x => x.LineNumber == lineNumber)
            .OrderBy(x => x.QueuePosition)
            .ToList();

        var queuePosition = lineOrders.Count == 0
            ? 1
            : lineOrders.Max(x => x.QueuePosition) + 1;

        var order = new ComponentProductionOrder
        {
            PlanetId = planet.Id,
            Planet = planet,
            LineNumber = lineNumber,
            QueuePosition = queuePosition,
            ComponentCode = component.Code,
            Quantity = quantity
        };

        if (lineOrders.Count == 0)
        {
            StartOrder(
                order,
                component,
                planet.ProductionComplexLevel,
                utcNow);
        }

        planet.ProductionOrders.Add(order);

        return new ProductionOrderResult(
            lineNumber,
            queuePosition,
            component.Code,
            quantity,
            materialsCost,
            deuteriumCost,
            order.StartedAt,
            order.CompletesAt);
    }

    public static int Process(
        Planet planet,
        DateTime utcNow)
    {
        var completedOrders = 0;

        for (var lineNumber = 1;
             lineNumber <= GetLineCount(planet);
             lineNumber++)
        {
            while (true)
            {
                var lineOrders = planet.ProductionOrders
                    .Where(x => x.LineNumber == lineNumber)
                    .OrderBy(x => x.QueuePosition)
                    .ToList();

                if (lineOrders.Count == 0)
                {
                    break;
                }

                var activeOrder = lineOrders[0];

                if (activeOrder.StartedAt is null)
                {
                    var definition =
                        GetRequiredComponent(
                            activeOrder.ComponentCode);

                    StartOrder(
                        activeOrder,
                        definition,
                        planet.ProductionComplexLevel,
                        utcNow);
                }

                if (activeOrder.CompletesAt > utcNow)
                {
                    break;
                }

                var completionTime =
                    activeOrder.CompletesAt!.Value;

                AddToInventory(
                    planet,
                    activeOrder.ComponentCode,
                    activeOrder.Quantity);

                planet.ProductionOrders.Remove(activeOrder);
                completedOrders++;

                var nextOrder = lineOrders
                    .Skip(1)
                    .FirstOrDefault();

                if (nextOrder is null)
                {
                    break;
                }

                var nextDefinition =
                    GetRequiredComponent(
                        nextOrder.ComponentCode);

                StartOrder(
                    nextOrder,
                    nextDefinition,
                    planet.ProductionComplexLevel,
                    completionTime);
            }
        }

        return completedOrders;
    }

    public static int GetLineCount(Planet planet)
    {
        return planet.ProductionComplexLevel;
    }

    private static void StartOrder(
        ComponentProductionOrder order,
        IComponentDefinition component,
        int complexLevel,
        DateTime startedAt)
    {
        var speedMultiplier =
            1m + (complexLevel - 1) * 0.1m;

        var durationSeconds = decimal.Ceiling(
            component.ProductionSeconds *
            order.Quantity /
            speedMultiplier);

        order.StartedAt = startedAt;
        order.CompletesAt = startedAt.AddSeconds(
            (double)durationSeconds);
    }

    private static void AddToInventory(
        Planet planet,
        string componentCode,
        int quantity)
    {
        var inventoryItem = planet.ComponentInventory
            .SingleOrDefault(x =>
                string.Equals(
                    x.ComponentCode,
                    componentCode,
                    StringComparison.OrdinalIgnoreCase));

        if (inventoryItem is null)
        {
            inventoryItem = new ComponentInventoryItem
            {
                PlanetId = planet.Id,
                Planet = planet,
                ComponentCode = componentCode
            };

            planet.ComponentInventory.Add(inventoryItem);
        }

        inventoryItem.Quantity += quantity;
    }

    private static IComponentDefinition GetRequiredComponent(
        string componentCode)
    {
        return StarterComponentCatalog.Find(componentCode)
            ?? throw new InvalidOperationException(
                $"Component '{componentCode}' does not exist.");
    }
}

public sealed record ProductionOrderResult(
    int LineNumber,
    int QueuePosition,
    string ComponentCode,
    int Quantity,
    decimal MaterialsCost,
    decimal DeuteriumCost,
    DateTime? StartedAt,
    DateTime? CompletesAt);
