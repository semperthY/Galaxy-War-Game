namespace Galaxy.Domain.Entities;

public class ShipBlueprintModule
{
    public Guid Id { get; set; }

    public Guid ShipBlueprintId { get; set; }

    public ShipBlueprint ShipBlueprint { get; set; } = null!;

    public string ComponentCode { get; set; } = null!;

    public int Quantity { get; set; }
}
