using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record EngineDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal InSystemSpeed,
    decimal InterSystemSpeed,
    decimal EnergyConsumption) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Engine;
}
