namespace Galaxy.Domain.Entities;

public class Ship
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid PlanetId { get; set; }

    public Planet Planet { get; set; } = null!;

    public Guid ShipBlueprintId { get; set; }

    public ShipBlueprint Blueprint { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public FleetShip? FleetShip { get; set; }
}
