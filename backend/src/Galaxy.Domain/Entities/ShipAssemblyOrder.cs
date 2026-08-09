namespace Galaxy.Domain.Entities;

public class ShipAssemblyOrder
{
    public Guid Id { get; set; }

    public Guid PlanetId { get; set; }

    public Planet Planet { get; set; } = null!;

    public Guid ShipBlueprintId { get; set; }

    public ShipBlueprint Blueprint { get; set; } = null!;

    public int QueuePosition { get; set; }

    public int Quantity { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletesAt { get; set; }
}
