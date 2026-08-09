using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;

namespace Galaxy.Application.ShipDesign;

public static class ShipBlueprintService
{
    public static ShipBlueprintCreationResult Create(
        Player player,
        string name,
        string hullCode,
        IReadOnlyCollection<ModuleSelection> modules,
        DateTime utcNow)
    {
        name = name.Trim();

        if (name.Length is < 3 or > 64)
        {
            throw new InvalidOperationException(
                "Blueprint name must contain from 3 to 64 characters.");
        }

        var design = ShipDesignCalculator.Calculate(
            hullCode,
            modules);

        var version = player.Blueprints
            .Where(x => string.Equals(
                x.Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Version)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var blueprint = new ShipBlueprint
        {
            PlayerId = player.Id,
            Player = player,
            Name = name,
            Version = version,
            HullCode = design.HullCode,
            CreatedAt = utcNow
        };

        foreach (var module in modules)
        {
            blueprint.Modules.Add(new ShipBlueprintModule
            {
                ShipBlueprint = blueprint,
                ComponentCode = module.ComponentCode,
                Quantity = module.Quantity
            });
        }

        player.Blueprints.Add(blueprint);

        return new ShipBlueprintCreationResult(
            blueprint,
            design);
    }
}

public sealed record ShipBlueprintCreationResult(
    ShipBlueprint Blueprint,
    ShipDesignResult Design);
