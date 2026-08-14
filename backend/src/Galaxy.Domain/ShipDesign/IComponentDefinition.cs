using Galaxy.Domain.Entities;

namespace Galaxy.Domain.ShipDesign;

public interface IComponentDefinition
{
    string Code { get; }

    string Name { get; }

    RaceType? Race { get; }

    ComponentType Type { get; }

    ComponentCost Cost { get; }

    int ProductionSeconds { get; }

    TechnologyType RequiredTechnology { get; }

    int RequiredTechnologyLevel { get; }
}

public sealed record ComponentCost(
    decimal Materials,
    decimal Deuterium);
