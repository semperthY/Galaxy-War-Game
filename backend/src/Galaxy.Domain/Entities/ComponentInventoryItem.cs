namespace Galaxy.Domain.Entities;

public class ComponentInventoryItem
{
    public Guid Id { get; set; }

    public Guid PlanetId { get; set; }

    public Planet Planet { get; set; } = null!;

    public string ComponentCode { get; set; } = null!;

    public int Quantity { get; set; }
}
