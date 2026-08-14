using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record ArmorDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal BonusStructuralIntegrity) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Armor;
}

public sealed record ShieldDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal ShieldCapacity,
    decimal EnergyConsumption) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Shield;
}

public sealed record ScannerDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal ScanRange,
    decimal EnergyConsumption,
    decimal CommandLoad) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Scanner;
}

public sealed record CargoHoldDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal CargoCapacity,
    decimal EnergyConsumption) : IComponentDefinition
{
    public ComponentType Type => ComponentType.CargoHold;
}

public sealed record LaserWeaponDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal ShieldDamage,
    decimal HullDamage,
    decimal EnergyConsumption,
    decimal CommandLoad) : IComponentDefinition
{
    public ComponentType Type => ComponentType.LaserWeapon;
}

public sealed record MissileWeaponDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal ShieldDamage,
    decimal HullDamage,
    decimal EnergyConsumption,
    decimal CommandLoad) : IComponentDefinition
{
    public ComponentType Type => ComponentType.MissileWeapon;
}

public sealed record MiningModuleDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal MiningRatePerMinute,
    decimal EnergyConsumption) : IComponentDefinition
{
    public ComponentType Type => ComponentType.MiningModule;
}
