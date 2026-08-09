using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record ColonyModuleDefinition(
    string Code,
    string Name,
    RaceType Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel)
    : IComponentDefinition
{
    public ComponentType Type => ComponentType.ColonyModule;
}
