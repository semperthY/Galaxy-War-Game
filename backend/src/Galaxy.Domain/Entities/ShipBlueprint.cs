namespace Galaxy.Domain.Entities;

public class ShipBlueprint
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Version { get; set; }

    public string HullCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public ICollection<ShipBlueprintModule> Modules { get; set; } =
        new List<ShipBlueprintModule>();
}
