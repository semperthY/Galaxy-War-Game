using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public sealed record HullDefinition(
    string Code,
    string Name,
    RaceType Race,
    decimal Volume,
    ComponentCost Cost,
    int ProductionSeconds,
    TechnologyType RequiredTechnology,
    int RequiredTechnologyLevel,
    decimal Capacity,
    decimal StructuralIntegrity) : IComponentDefinition
{
    public ComponentType Type => ComponentType.Hull;
}
