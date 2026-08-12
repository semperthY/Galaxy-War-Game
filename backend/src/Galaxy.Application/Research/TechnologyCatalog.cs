using Galaxy.Domain.Entities;

namespace Galaxy.Application.Research;

public static class TechnologyCatalog
{
    private static readonly IReadOnlyDictionary<TechnologyType, TechnologyDefinition>
        Definitions = CreateDefinitions();

    public static IReadOnlyCollection<TechnologyDefinition> GetAll() =>
        Definitions.Values.ToList();

    public static TechnologyDefinition Get(TechnologyType technology) =>
        Definitions.TryGetValue(technology, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(technology));

    private static IReadOnlyDictionary<TechnologyType, TechnologyDefinition>
        CreateDefinitions()
    {
        var definitions = new[]
        {
            D(TechnologyType.MaterialsScience, "TEC-MAT", "Материаловедение", "Фундаментальные науки",
                L(1, 300, 0, 5, 1),
                L(2, 800, 40, 12, 2),
                L(3, 2200, 150, 30, 4, R(TechnologyType.IndustrialSystems, 1))),

            D(TechnologyType.EnergySystems, "TEC-ENE", "Энергетика", "Фундаментальные науки",
                L(1, 250, 80, 6, 1, R(TechnologyType.MaterialsScience, 1)),
                L(2, 700, 280, 14, 2, R(TechnologyType.ReactorSystems, 1)),
                L(3, 2000, 900, 35, 4, R(TechnologyType.ReactorSystems, 2), R(TechnologyType.ComputingSystems, 2)),
                L(4, 6000, 3200, 90, 7, R(TechnologyType.SpatialPhysics, 2), R(TechnologyType.ReactorSystems, 3))),

            D(TechnologyType.ComputingSystems, "TEC-CMP", "Вычислительные системы", "Фундаментальные науки",
                L(1, 220, 40, 5, 1, R(TechnologyType.MaterialsScience, 1)),
                L(2, 650, 150, 12, 2, R(TechnologyType.Electronics, 1)),
                L(3, 1800, 500, 30, 4, R(TechnologyType.Electronics, 2)),
                L(4, 5200, 1800, 75, 7, R(TechnologyType.Electronics, 3), R(TechnologyType.EnergySystems, 3))),

            D(TechnologyType.SpatialPhysics, "TEC-SPA", "Пространственная физика", "Фундаментальные науки",
                L(1, 900, 400, 20, 3, R(TechnologyType.EnergySystems, 2), R(TechnologyType.ComputingSystems, 2), R(TechnologyType.Electronics, 1)),
                L(2, 2600, 1400, 45, 5, R(TechnologyType.FieldDefense, 1), R(TechnologyType.LaserSystems, 2)),
                L(3, 7000, 4500, 100, 8, R(TechnologyType.EngineSystems, 3), R(TechnologyType.Electronics, 3))),

            D(TechnologyType.ShipEngineering, "TEC-SHP", "Корабельная инженерия", "Инженерные технологии",
                L(1, 350, 20, 5, 1, R(TechnologyType.MaterialsScience, 1)),
                L(2, 1000, 100, 15, 2, R(TechnologyType.MaterialsScience, 2), R(TechnologyType.IndustrialSystems, 1)),
                L(3, 3000, 400, 40, 5, R(TechnologyType.MaterialsScience, 3), R(TechnologyType.EngineSystems, 2), R(TechnologyType.Electronics, 2))),

            D(TechnologyType.EngineSystems, "TEC-ENG", "Двигательные системы", "Инженерные технологии",
                L(1, 300, 100, 6, 1, R(TechnologyType.EnergySystems, 1), R(TechnologyType.MaterialsScience, 1)),
                L(2, 900, 350, 18, 2, R(TechnologyType.EnergySystems, 2), R(TechnologyType.ComputingSystems, 1)),
                L(3, 2800, 1400, 45, 5, R(TechnologyType.EnergySystems, 3), R(TechnologyType.SpatialPhysics, 1), R(TechnologyType.Electronics, 2))),

            D(TechnologyType.ReactorSystems, "TEC-RCT", "Реакторные системы", "Инженерные технологии",
                L(1, 280, 100, 6, 1, R(TechnologyType.EnergySystems, 1)),
                L(2, 850, 400, 15, 2, R(TechnologyType.EnergySystems, 2), R(TechnologyType.MaterialsScience, 2)),
                L(3, 2600, 1500, 40, 5, R(TechnologyType.EnergySystems, 3), R(TechnologyType.ComputingSystems, 2))),

            D(TechnologyType.Electronics, "TEC-ELC", "Корабельная электроника", "Инженерные технологии",
                L(1, 250, 50, 5, 1, R(TechnologyType.ComputingSystems, 1)),
                L(2, 750, 200, 15, 2, R(TechnologyType.ComputingSystems, 2), R(TechnologyType.EnergySystems, 2)),
                L(3, 2200, 750, 35, 5, R(TechnologyType.ComputingSystems, 3), R(TechnologyType.SpatialPhysics, 1)),
                L(4, 6800, 2800, 90, 8, R(TechnologyType.ComputingSystems, 4), R(TechnologyType.EnergySystems, 4), R(TechnologyType.SpatialPhysics, 3))),

            D(TechnologyType.IndustrialSystems, "TEC-IND", "Промышленные системы", "Инженерные технологии",
                L(1, 300, 20, 6, 1, R(TechnologyType.MaterialsScience, 1), R(TechnologyType.ComputingSystems, 1)),
                L(2, 1000, 160, 18, 2, R(TechnologyType.MaterialsScience, 2), R(TechnologyType.EnergySystems, 2), R(TechnologyType.Electronics, 1)),
                L(3, 3200, 700, 45, 5, R(TechnologyType.MaterialsScience, 3), R(TechnologyType.Electronics, 2), R(TechnologyType.ShipEngineering, 2))),

            D(TechnologyType.FieldDefense, "TEC-SHD", "Полевая защита", "Военные технологии",
                L(1, 500, 250, 12, 3, R(TechnologyType.SpatialPhysics, 1), R(TechnologyType.ReactorSystems, 2)),
                L(2, 1500, 800, 30, 4, R(TechnologyType.EnergySystems, 3), R(TechnologyType.Electronics, 2)),
                L(3, 4500, 2800, 70, 6, R(TechnologyType.SpatialPhysics, 2), R(TechnologyType.ReactorSystems, 3), R(TechnologyType.Electronics, 3))),

            D(TechnologyType.LaserSystems, "TEC-LAS", "Лазерные системы", "Военные технологии",
                L(1, 300, 80, 6, 1, R(TechnologyType.EnergySystems, 1), R(TechnologyType.ComputingSystems, 1)),
                L(2, 900, 300, 18, 2, R(TechnologyType.EnergySystems, 2), R(TechnologyType.Electronics, 1)),
                L(3, 3000, 1200, 45, 5, R(TechnologyType.EnergySystems, 3), R(TechnologyType.SpatialPhysics, 1), R(TechnologyType.Electronics, 2))),

            D(TechnologyType.MissileSystems, "TEC-MSL", "Ракетные системы", "Военные технологии",
                L(1, 350, 30, 6, 1, R(TechnologyType.MaterialsScience, 1), R(TechnologyType.ComputingSystems, 1)),
                L(2, 1000, 120, 18, 2, R(TechnologyType.MaterialsScience, 2), R(TechnologyType.Electronics, 1)),
                L(3, 3400, 500, 45, 5, R(TechnologyType.MaterialsScience, 3), R(TechnologyType.Electronics, 2), R(TechnologyType.IndustrialSystems, 2))),

            D(TechnologyType.SpecialSystems, "TEC-SPC", "Специальные системы", "Военные технологии",
                L(1, 700, 250, 15, 3, R(TechnologyType.Electronics, 2), R(TechnologyType.EnergySystems, 2)),
                L(2, 2200, 1000, 40, 5, R(TechnologyType.SpatialPhysics, 1), R(TechnologyType.ComputingSystems, 3), R(TechnologyType.FieldDefense, 1))),

            D(TechnologyType.Colonization, "TEC-COL", "Колонизационные технологии", "Имперские технологии",
                L(1, 1500, 600, 30, 4, R(TechnologyType.ShipEngineering, 2), R(TechnologyType.IndustrialSystems, 2), R(TechnologyType.SpatialPhysics, 1), R(TechnologyType.Electronics, 1))),

            D(TechnologyType.ResearchCoordination, "TEC-RSC", "Научная координация", "Имперские технологии",
                L(1, 3500, 1200, 60, 5, R(TechnologyType.ComputingSystems, 3), R(TechnologyType.EnergySystems, 2), R(TechnologyType.Electronics, 2)),
                L(2, 14000, 8000, 180, 9, R(TechnologyType.ComputingSystems, 4), R(TechnologyType.Electronics, 4), R(TechnologyType.EnergySystems, 4)))
        };

        return definitions.ToDictionary(x => x.Technology);
    }

    private static TechnologyDefinition D(
        TechnologyType technology,
        string code,
        string name,
        string category,
        params TechnologyLevelDefinition[] levels) =>
        new(technology, code, name, category, levels.ToDictionary(x => x.Level));

    private static TechnologyLevelDefinition L(
        int level,
        decimal materials,
        decimal deuterium,
        int minutes,
        int laboratoryLevel,
        params TechnologyRequirement[] requirements) =>
        new(level, new ResearchCost(materials, deuterium), TimeSpan.FromMinutes(minutes), laboratoryLevel, requirements);

    private static TechnologyRequirement R(TechnologyType technology, int level) =>
        new(technology, level);
}

public sealed record TechnologyDefinition(
    TechnologyType Technology,
    string Code,
    string Name,
    string Category,
    IReadOnlyDictionary<int, TechnologyLevelDefinition> Levels)
{
    public int MaxLevel => Levels.Keys.Max();
}

public sealed record TechnologyLevelDefinition(
    int Level,
    ResearchCost Cost,
    TimeSpan Duration,
    int RequiredLaboratoryLevel,
    IReadOnlyCollection<TechnologyRequirement> Requirements);
