using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.Components;

public static class StarterComponentCatalog
{
    private static readonly IReadOnlyList<IComponentDefinition>
        AllComponents = CreateComponents();

    private static readonly IReadOnlyList<IComponentDefinition>
        Components = AllComponents
            .Where(x => string.Equals(
                x.Code,
                x.Code.ToUpperInvariant(),
                StringComparison.Ordinal))
            .ToList();

    public static IReadOnlyList<IComponentDefinition> GetAll()
    {
        return Components;
    }

    public static IReadOnlyList<IComponentDefinition> GetResolvable()
    {
        return AllComponents;
    }

    public static IComponentDefinition? Find(string code)
    {
        return AllComponents.SingleOrDefault(x =>
            string.Equals(
                x.Code,
                code,
                StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<IComponentDefinition> GetForRace(
        RaceType race)
    {
        return Components
            .Where(x => x.Race is null || x.Race == race)
            .ToList();
    }

    private static IReadOnlyList<IComponentDefinition>
        CreateComponents()
    {
        var components = new List<IComponentDefinition>();

        AddUniversalComponents(components);
        AddUniqueRaceComponents(components);

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

    private static void AddUniversalComponents(
        ICollection<IComponentDefinition> components)
    {
        components.Add(new HullDefinition(
            "HUL-01", "Лёгкий каркас «Игла»", null,
            new ComponentCost(400m, 40m), 240,
            TechnologyType.ShipEngineering, 1, 50m, 100m));
        components.Add(new HullDefinition(
            "HUL-02", "Эскортный каркас «Коршун»", null,
            new ComponentCost(900m, 100m), 600,
            TechnologyType.ShipEngineering, 2, 90m, 200m));
        components.Add(new HullDefinition(
            "HUL-03", "Линейный каркас «Вектор»", null,
            new ComponentCost(2200m, 280m), 1200,
            TechnologyType.ShipEngineering, 3, 160m, 400m));
        components.Add(new HullDefinition(
            "HUL-04", "Тяжёлый каркас «Меридиан»", null,
            new ComponentCost(5200m, 800m), 2400,
            TechnologyType.ShipEngineering, 3, 280m, 800m));
        components.Add(new HullDefinition(
            "HUL-05", "Капитальный каркас «Атлас»", null,
            new ComponentCost(12500m, 2400m), 4800,
            TechnologyType.ShipEngineering, 4, 500m, 1600m));
        components.Add(new HullDefinition(
            "HUL-06", "Сверхтяжёлый каркас «Левиафан»", null,
            new ComponentCost(30000m, 7000m), 9000,
            TechnologyType.ShipEngineering, 4, 900m, 3200m));

        components.Add(new EngineDefinition(
            "ENG-01", "Химический двигатель «Вектор»", null,
            8m, new ComponentCost(160m, 20m), 120,
            TechnologyType.EngineSystems, 1,
            120m, 25m, 4m));
        components.Add(new EngineDefinition(
            "ENG-02", "Ионный двигатель «Астер»", null,
            10m, new ComponentCost(250m, 50m), 180,
            TechnologyType.EngineSystems, 1,
            65m, 80m, 8m));
        components.Add(new EngineDefinition(
            "ENG-03", "Плазменный двигатель «Спираль»", null,
            12m, new ComponentCost(480m, 120m), 300,
            TechnologyType.EngineSystems, 2,
            100m, 100m, 18m));

        components.Add(new ReactorDefinition(
            "RCT-01", "Изотопное ядро «Искра»", null,
            6m, new ComponentCost(120m, 30m), 120,
            TechnologyType.ReactorSystems, 1, 22m));
        components.Add(new ReactorDefinition(
            "RCT-02", "Термоядерное ядро «Гелиос»", null,
            10m, new ComponentCost(300m, 100m), 240,
            TechnologyType.ReactorSystems, 2, 50m));

        components.Add(new ControlSystemDefinition(
            "CTL-01", "Навигационное ядро «Следопыт»", null,
            5m, new ComponentCost(150m, 25m), 150,
            TechnologyType.Electronics, 1, 30m, 4m));
        components.Add(new ControlSystemDefinition(
            "CTL-02", "Тактическое ядро «Эгида»", null,
            8m, new ComponentCost(380m, 80m), 300,
            TechnologyType.Electronics, 2, 60m, 8m));

        components.Add(new ScannerDefinition(
            "SNS-01", "Навигационный радар «Следопыт»", null,
            3m, new ComponentCost(80m, 10m), 90,
            TechnologyType.Electronics, 1,
            30m, 3m, 4m));
        components.Add(new ScannerDefinition(
            "SNS-02", "Тактический сканер «Рысь»", null,
            5m, new ComponentCost(180m, 40m), 150,
            TechnologyType.Electronics, 2,
            45m, 7m, 7m));

        components.Add(new MiningModuleDefinition(
            "IND-01", "Малый гравитационный захват «Клещ»", null,
            8m, new ComponentCost(220m, 40m), 180,
            TechnologyType.IndustrialSystems, 1, 5m, 10m));
        components.Add(new CargoHoldDefinition(
            "IND-05", "Стандартный грузовой отсек", null,
            10m, new ComponentCost(180m, 10m), 120,
            TechnologyType.IndustrialSystems, 1, 100m, 0m));
        components.Add(new CargoHoldDefinition(
            "IND-06", "Уплотнённое грузовое хранилище", null,
            16m, new ComponentCost(480m, 60m), 300,
            TechnologyType.IndustrialSystems, 2, 220m, 2m));
        components.Add(new ColonyModuleDefinition(
            "IND-08", "Колонизационное ядро «Пионер»", null,
            18m, new ComponentCost(1800m, 700m), 1800,
            TechnologyType.Colonization, 1, 8m));

        components.Add(new ArmorDefinition(
            "ARM-01", "Керамитовая броня", null,
            7m, new ComponentCost(220m, 10m), 150,
            TechnologyType.ShipEngineering, 1, 60m));
        components.Add(new ArmorDefinition(
            "ARM-02", "Абляционная броня", null,
            10m, new ComponentCost(420m, 20m), 240,
            TechnologyType.ShipEngineering, 2, 100m));

        components.Add(new ShieldDefinition(
            "SHD-01", "Лёгкий дефлектор «Мерцание»", null,
            7m, new ComponentCost(180m, 70m), 180,
            TechnologyType.FieldDefense, 1, 50m, 12m));
        components.Add(new ShieldDefinition(
            "SHD-02", "Импульсный барьер", null,
            10m, new ComponentCost(380m, 170m), 300,
            TechnologyType.FieldDefense, 2, 90m, 20m));

        components.Add(new LaserWeaponDefinition(
            "LAS-01", "Лазер «Игла»", null,
            3m, new ComponentCost(100m, 20m), 90,
            TechnologyType.LaserSystems, 1,
            8m, 2m, 5m, 4m));
        components.Add(new LaserWeaponDefinition(
            "LAS-02", "Импульсный лазер «Мерцание»", null,
            5m, new ComponentCost(220m, 60m), 150,
            TechnologyType.LaserSystems, 2,
            16m, 8m, 10m, 7m));
        components.Add(new LaserWeaponDefinition(
            "LAS-03", "Призменный излучатель", null,
            8m, new ComponentCost(480m, 150m), 300,
            TechnologyType.LaserSystems, 3,
            28m, 15m, 17m, 12m));

        components.Add(new MissileWeaponDefinition(
            "MSL-01", "Микроячейка «Оса»", null,
            4m, new ComponentCost(130m, 10m), 90,
            TechnologyType.MissileSystems, 1,
            5m, 12m, 2m, 5m));
        components.Add(new MissileWeaponDefinition(
            "MSL-02", "Лёгкая установка «Копейщик»", null,
            7m, new ComponentCost(300m, 30m), 180,
            TechnologyType.MissileSystems, 2,
            10m, 22m, 3m, 8m));
        components.Add(new MissileWeaponDefinition(
            "MSL-03", "Ударный блок «Коготь»", null,
            14m, new ComponentCost(700m, 90m), 360,
            TechnologyType.MissileSystems, 3,
            18m, 45m, 5m, 14m));
    }

    private static void AddUniqueRaceComponents(
        ICollection<IComponentDefinition> components)
    {
        components.Add(new ArmorDefinition(
            "ARM-H01", "Броня «Оплот»", RaceType.Humans,
            16m, new ComponentCost(1300m, 100m), 660,
            TechnologyType.ShipEngineering, 3, 210m));
        components.Add(new ControlSystemDefinition(
            "CTL-H01", "Ядро «Координатор»", RaceType.Humans,
            14m, new ComponentCost(1250m, 320m), 720,
            TechnologyType.Electronics, 3, 135m, 22m));

        components.Add(new ReactorDefinition(
            "RCT-S01", "Реактор «Логос»", RaceType.Synthetics,
            11m, new ComponentCost(650m, 260m), 600,
            TechnologyType.ReactorSystems, 3, 62m));
        components.Add(new ScannerDefinition(
            "SNS-S01", "Сканер «Аналитик»", RaceType.Synthetics,
            8m, new ComponentCost(900m, 300m), 900,
            TechnologyType.Electronics, 3,
            95m, 9m, 15m));

        components.Add(new EngineDefinition(
            "ENG-I01", "Двигатель «Жало»", RaceType.Insectoids,
            9m, new ComponentCost(520m, 130m), 420,
            TechnologyType.EngineSystems, 2,
            145m, 55m, 12m));
        components.Add(new CargoHoldDefinition(
            "IND-I01", "Ячеистый трюм «Соты»", RaceType.Insectoids,
            12m, new ComponentCost(520m, 70m), 420,
            TechnologyType.IndustrialSystems, 2, 180m, 3m));

        components.Add(new ShieldDefinition(
            "SHD-E01", "Щит «Сияние»", RaceType.EnergyForms,
            18m, new ComponentCost(1700m, 1000m), 900,
            TechnologyType.FieldDefense, 4, 230m, 55m));
        components.Add(new LaserWeaponDefinition(
            "LAS-E01", "Излучатель «Протуберанец»", RaceType.EnergyForms,
            13m, new ComponentCost(1800m, 900m), 900,
            TechnologyType.LaserSystems, 4,
            55m, 15m, 42m, 24m));
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
            TechnologyType.Colonization,
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
            TechnologyType.ShipEngineering,
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
            TechnologyType.EngineSystems,
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
            TechnologyType.ReactorSystems,
            1,
            reactorOutput));

        components.Add(new ControlSystemDefinition(
            $"{prefix}-control-1",
            $"{race} Control System",
            race,
            controlVolume,
            new ComponentCost(80m, 20m),
            10,
            TechnologyType.Electronics,
            1,
            commandRating,
            controlEnergy));
    }
}
