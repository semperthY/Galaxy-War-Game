using Galaxy.Application.Components;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.ShipDesign;

public static class ShipDesignCalculator
{
    public static ShipDesignResult Calculate(
        string hullCode,
        IReadOnlyCollection<ModuleSelection> modules)
    {
        var hull = StarterComponentCatalog.Find(hullCode)
            as HullDefinition
            ?? throw new InvalidOperationException(
                "A valid hull is required.");

        if (modules.Count == 0)
        {
            throw new InvalidOperationException(
                "Ship modules are required.");
        }

        var resolvedModules = modules
            .SelectMany(selection =>
            {
                if (selection.Quantity < 1)
                {
                    throw new InvalidOperationException(
                        "Module quantity must be positive.");
                }

                var definition =
                    StarterComponentCatalog.Find(
                        selection.ComponentCode)
                    ?? throw new InvalidOperationException(
                        $"Component '{selection.ComponentCode}' " +
                        "does not exist.");

                if (definition is HullDefinition)
                {
                    throw new InvalidOperationException(
                        "A hull cannot be installed as a module.");
                }

                return Enumerable.Repeat(
                    definition,
                    selection.Quantity);
            })
            .ToList();

        RequireModule<EngineDefinition>(
            resolvedModules,
            "An engine is required.");

        RequireModule<ReactorDefinition>(
            resolvedModules,
            "A reactor is required.");

        RequireModule<ControlSystemDefinition>(
            resolvedModules,
            "A control system is required.");

        var usedVolume = resolvedModules.Sum(GetVolume);

        if (usedVolume > hull.Capacity)
        {
            throw new InvalidOperationException(
                "Installed modules exceed hull capacity.");
        }

        var energyProduction = resolvedModules
            .OfType<ReactorDefinition>()
            .Sum(x => x.EnergyOutput);

        var energyConsumption =
            resolvedModules
                .OfType<EngineDefinition>()
                .Sum(x => x.EnergyConsumption) +
            resolvedModules
                .OfType<ControlSystemDefinition>()
                .Sum(x => x.EnergyConsumption);

        if (energyConsumption > energyProduction)
        {
            throw new InvalidOperationException(
                "Installed modules require more energy " +
                "than the reactors produce.");
        }

        var inSystemSpeed = resolvedModules
            .OfType<EngineDefinition>()
            .Sum(x => x.InSystemSpeed);

        var interSystemSpeed = resolvedModules
            .OfType<EngineDefinition>()
            .Sum(x => x.InterSystemSpeed);

        var commandRating = resolvedModules
            .OfType<ControlSystemDefinition>()
            .Sum(x => x.CommandRating);

        var requiredComponents = modules
            .Select(x => new RequiredComponent(
                x.ComponentCode,
                x.Quantity))
            .Prepend(new RequiredComponent(
                hull.Code,
                1))
            .ToList();

        return new ShipDesignResult(
            hull.Code,
            hull.Capacity,
            usedVolume,
            hull.Capacity - usedVolume,
            hull.StructuralIntegrity,
            energyProduction,
            energyConsumption,
            energyProduction - energyConsumption,
            inSystemSpeed,
            interSystemSpeed,
            commandRating,
            requiredComponents);
    }

    private static decimal GetVolume(
        IComponentDefinition component)
    {
        return component switch
        {
            EngineDefinition engine => engine.Volume,
            ReactorDefinition reactor => reactor.Volume,
            ControlSystemDefinition control => control.Volume,

            _ => throw new InvalidOperationException(
                $"Component type '{component.Type}' " +
                "cannot be installed yet.")
        };
    }

    private static void RequireModule<T>(
        IEnumerable<IComponentDefinition> modules,
        string error)
        where T : class, IComponentDefinition
    {
        if (!modules.OfType<T>().Any())
        {
            throw new InvalidOperationException(error);
        }
    }
}

public sealed record ModuleSelection(
    string ComponentCode,
    int Quantity);

public sealed record RequiredComponent(
    string ComponentCode,
    int Quantity);

public sealed record ShipDesignResult(
    string HullCode,
    decimal HullCapacity,
    decimal UsedVolume,
    decimal FreeVolume,
    decimal StructuralIntegrity,
    decimal EnergyProduction,
    decimal EnergyConsumption,
    decimal FreeEnergy,
    decimal InSystemSpeed,
    decimal InterSystemSpeed,
    decimal CommandRating,
    IReadOnlyCollection<RequiredComponent> RequiredComponents);
