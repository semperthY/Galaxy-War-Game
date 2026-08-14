using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record ControlSystemDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal CommandRating,
    decimal EnergyConsumption) : IComponentDefinition
{
    public ComponentType Type => ComponentType.ControlSystem;
}
