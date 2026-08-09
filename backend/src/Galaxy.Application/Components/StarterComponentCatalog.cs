using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.Components;

public static class StarterComponentCatalog
{
    private static readonly IReadOnlyList<IComponentDefinition>
        Components = CreateComponents();

    public static IReadOnlyList<IComponentDefinition> GetAll()
    {
        return Components;
    }

    public static IComponentDefinition? Find(string code)
    {
        return Components.SingleOrDefault(x =>
            string.Equals(
                x.Code,
                code,
                StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<IComponentDefinition> GetForRace(
        RaceType race)
    {
        return Components
            .Where(x => x.Race == race)
            .ToList();
    }

    private static IReadOnlyList<IComponentDefinition>
        CreateComponents()
    {
        var components = new List<IComponentDefinition>();

        AddRaceComponents(
            components,
            RaceType.Humans,
            "humans",
            hullCapacity: 50m,
            hullIntegrity: 100m,
            engineVolume: 12m,
            inSystemSpeed: 100m,
            interSystemSpeed: 80m,
            engineEnergy: 20m,
            reactorVolume: 10m,
            reactorOutput: 50m,
            controlVolume: 5m,
            commandRating: 50m,
            controlEnergy: 5m);

        AddRaceComponents(
            components,
            RaceType.Synthetics,
            "synthetics",
            hullCapacity: 48m,
            hullIntegrity: 110m,
            engineVolume: 12m,
            inSystemSpeed: 95m,
            interSystemSpeed: 78m,
            engineEnergy: 14m,
            reactorVolume: 9m,
            reactorOutput: 46m,
            controlVolume: 4m,
            commandRating: 55m,
            controlEnergy: 3m);

        AddRaceComponents(
            components,
            RaceType.Insectoids,
            "insectoids",
            hullCapacity: 46m,
            hullIntegrity: 90m,
            engineVolume: 9m,
            inSystemSpeed: 105m,
            interSystemSpeed: 75m,
            engineEnergy: 19m,
            reactorVolume: 8m,
            reactorOutput: 42m,
            controlVolume: 4m,
            commandRating: 42m,
            controlEnergy: 4m);

        AddRaceComponents(
            components,
            RaceType.EnergyForms,
            "energyforms",
            hullCapacity: 52m,
            hullIntegrity: 85m,
            engineVolume: 14m,
            inSystemSpeed: 115m,
            interSystemSpeed: 95m,
            engineEnergy: 30m,
            reactorVolume: 12m,
            reactorOutput: 70m,
            controlVolume: 6m,
            commandRating: 60m,
            controlEnergy: 8m);

        AddColonyModule(
            components,
            RaceType.Humans,
            "humans",
            volume: 18m,
            materials: 220m,
            deuterium: 80m,
            productionSeconds: 25);

        AddColonyModule(
            components,
            RaceType.Synthetics,
            "synthetics",
            volume: 16m,
            materials: 260m,
            deuterium: 90m,
            productionSeconds: 30);

        AddColonyModule(
            components,
            RaceType.Insectoids,
            "insectoids",
            volume: 20m,
            materials: 180m,
            deuterium: 65m,
            productionSeconds: 20);

        AddColonyModule(
            components,
            RaceType.EnergyForms,
            "energyforms",
            volume: 14m,
            materials: 320m,
            deuterium: 130m,
            productionSeconds: 35);
        return components;
    }

    private static void AddColonyModule(
        ICollection<IComponentDefinition> components,
        RaceType race,
        string prefix,
        decimal volume,
        decimal materials,
        decimal deuterium,
        int productionSeconds)
    {
        components.Add(new ColonyModuleDefinition(
            $"{prefix}-colony-1",
            $"{race} Colony Module",
            race,
            volume,
            new ComponentCost(materials, deuterium),
            productionSeconds,
            TechnologyType.ComponentEngineering,
            1));
    }
    private static void AddRaceComponents(
        ICollection<IComponentDefinition> components,
        RaceType race,
        string prefix,
        decimal hullCapacity,
        decimal hullIntegrity,
        decimal engineVolume,
        decimal inSystemSpeed,
        decimal interSystemSpeed,
        decimal engineEnergy,
        decimal reactorVolume,
        decimal reactorOutput,
        decimal controlVolume,
        decimal commandRating,
        decimal controlEnergy)
    {
        components.Add(new HullDefinition(
            $"{prefix}-hull-1",
            $"{race} Light Hull",
            race,
            new ComponentCost(180m, 10m),
            20,
            TechnologyType.MaterialsScience,
            1,
            hullCapacity,
            hullIntegrity));

        components.Add(new EngineDefinition(
            $"{prefix}-engine-1",
            $"{race} Basic Engine",
            race,
            engineVolume,
            new ComponentCost(100m, 35m),
            15,
            TechnologyType.Propulsion,
            1,
            inSystemSpeed,
            interSystemSpeed,
            engineEnergy));

        components.Add(new ReactorDefinition(
            $"{prefix}-reactor-1",
            $"{race} Basic Reactor",
            race,
            reactorVolume,
            new ComponentCost(110m, 40m),
            15,
            TechnologyType.EnergySystems,
            1,
            reactorOutput));

        components.Add(new ControlSystemDefinition(
            $"{prefix}-control-1",
            $"{race} Control System",
            race,
            controlVolume,
            new ComponentCost(80m, 20m),
            10,
            TechnologyType.ControlSystems,
            1,
            commandRating,
            controlEnergy));
    }
}


