using Galaxy.Application.Components;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.ShipDesign;

public static class ShipDesignCalculator
{
    public static ShipDesignResult Calculate(
        string hullCode,
        IReadOnlyCollection<ModuleSelection> modules) =>
        Calculate(
            hullCode,
            modules,
            StarterComponentCatalog.GetAll());

    public static ShipDesignResult Calculate(
        string hullCode,
        IReadOnlyCollection<ModuleSelection> modules,
        IReadOnlyCollection<IComponentDefinition> catalog)
    {
        var hull = FindComponent(catalog, hullCode)
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
                    FindComponent(
                        catalog,
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

        var energyConsumption = resolvedModules
            .Sum(GetEnergyConsumption);

        if (energyConsumption > energyProduction)
        {
            throw new InvalidOperationException(
                "Installed modules require more energy " +
                "than the reactors produce.");
        }

        var commandRating = resolvedModules
            .OfType<ControlSystemDefinition>()
            .Sum(x => x.CommandRating);

        var commandLoad = resolvedModules
            .Sum(GetCommandLoad);

        if (commandLoad > commandRating)
        {
            throw new InvalidOperationException(
                "Installed active systems exceed " +
                "control system command capacity.");
        }

        var inSystemSpeed = resolvedModules
            .OfType<EngineDefinition>()
            .Sum(x => x.InSystemSpeed);

        var interSystemSpeed = resolvedModules
            .OfType<EngineDefinition>()
            .Sum(x => x.InterSystemSpeed);

        var structuralIntegrity =
            hull.StructuralIntegrity +
            resolvedModules
                .OfType<ArmorDefinition>()
                .Sum(x => x.BonusStructuralIntegrity);

        var shieldCapacity = resolvedModules
            .OfType<ShieldDefinition>()
            .Sum(x => x.ShieldCapacity);

        var scanRange = resolvedModules
            .OfType<ScannerDefinition>()
            .Select(x => x.ScanRange)
            .DefaultIfEmpty(0m)
            .Max();

        var cargoCapacity = resolvedModules
            .OfType<CargoHoldDefinition>()
            .Sum(x => x.CargoCapacity);

        var miningRatePerMinute = resolvedModules
            .OfType<MiningModuleDefinition>()
            .Sum(x => x.MiningRatePerMinute);

        var shieldDamage =
            resolvedModules
                .OfType<LaserWeaponDefinition>()
                .Sum(x => x.ShieldDamage) +
            resolvedModules
                .OfType<MissileWeaponDefinition>()
                .Sum(x => x.ShieldDamage);

        var hullDamage =
            resolvedModules
                .OfType<LaserWeaponDefinition>()
                .Sum(x => x.HullDamage) +
            resolvedModules
                .OfType<MissileWeaponDefinition>()
                .Sum(x => x.HullDamage);

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
            structuralIntegrity,
            shieldCapacity,
            energyProduction,
            energyConsumption,
            energyProduction - energyConsumption,
            inSystemSpeed,
            interSystemSpeed,
            commandRating,
            commandLoad,
            commandRating - commandLoad,
            scanRange,
            cargoCapacity,
            shieldDamage,
            hullDamage,
            requiredComponents,
            miningRatePerMinute);
    }

    private static IComponentDefinition? FindComponent(
        IEnumerable<IComponentDefinition> catalog,
        string code) =>
        catalog.SingleOrDefault(x =>
            string.Equals(
                x.Code,
                code,
                StringComparison.OrdinalIgnoreCase));

    private static decimal GetVolume(
        IComponentDefinition component)
    {
        return component switch
        {
            EngineDefinition engine => engine.Volume,
            ReactorDefinition reactor => reactor.Volume,
            ControlSystemDefinition control => control.Volume,
            ColonyModuleDefinition colony => colony.Volume,
            ArmorDefinition armor => armor.Volume,
            ShieldDefinition shield => shield.Volume,
            ScannerDefinition scanner => scanner.Volume,
            CargoHoldDefinition cargo => cargo.Volume,
            LaserWeaponDefinition laser => laser.Volume,
            MissileWeaponDefinition missile => missile.Volume,
            MiningModuleDefinition mining => mining.Volume,

            _ => throw new InvalidOperationException(
                $"Component type '{component.Type}' " +
                "cannot be installed yet.")
        };
    }

    private static decimal GetEnergyConsumption(
        IComponentDefinition component) =>
        component switch
        {
            EngineDefinition engine => engine.EnergyConsumption,
            ControlSystemDefinition control => control.EnergyConsumption,
            ColonyModuleDefinition colony => colony.EnergyConsumption,
            ShieldDefinition shield => shield.EnergyConsumption,
            ScannerDefinition scanner => scanner.EnergyConsumption,
            CargoHoldDefinition cargo => cargo.EnergyConsumption,
            LaserWeaponDefinition laser => laser.EnergyConsumption,
            MissileWeaponDefinition missile => missile.EnergyConsumption,
            MiningModuleDefinition mining => mining.EnergyConsumption,
            _ => 0m
        };

    private static decimal GetCommandLoad(
        IComponentDefinition component) =>
        component switch
        {
            ScannerDefinition scanner => scanner.CommandLoad,
            LaserWeaponDefinition laser => laser.CommandLoad,
            MissileWeaponDefinition missile => missile.CommandLoad,
            _ => 0m
        };

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
    decimal ShieldCapacity,
    decimal EnergyProduction,
    decimal EnergyConsumption,
    decimal FreeEnergy,
    decimal InSystemSpeed,
    decimal InterSystemSpeed,
    decimal CommandRating,
    decimal CommandLoad,
    decimal FreeCommandRating,
    decimal ScanRange,
    decimal CargoCapacity,
    decimal ShieldDamage,
    decimal HullDamage,
    IReadOnlyCollection<RequiredComponent> RequiredComponents,
    decimal MiningRatePerMinute);
