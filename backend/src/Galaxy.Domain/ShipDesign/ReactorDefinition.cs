using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record ReactorDefinition(
    string Code,
    string Name,
    RaceType? Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal EnergyOutput) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Reactor;
}
