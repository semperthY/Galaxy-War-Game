namespace Galaxy.Domain.Entities;

public class Planet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public decimal Materials { get; set; }

    public decimal Deuterium { get; set; }

    public int MaterialsExtractorLevel { get; set; }

    public int DeuteriumExtractorLevel { get; set; }

    public int PowerPlantLevel { get; set; }

    public int WarehouseLevel { get; set; }

    public int ResearchLaboratoryLevel { get; set; }

    public int ProductionComplexLevel { get; set; }

    public int AssemblyComplexLevel { get; set; }

    public int BuildingSiteCapacity { get; set; }

    public DateTime ResourcesUpdatedAt { get; set; }

    public BuildingType? QueuedBuilding { get; set; }

    public int? QueuedBuildingLevel { get; set; }

    public DateTime? BuildingCompletesAt { get; set; }

    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }

    public Guid StarSystemId { get; set; }

    public StarSystem StarSystem { get; set; } = null!;

    public ICollection<ComponentInventoryItem> ComponentInventory { get; set; } =
        new List<ComponentInventoryItem>();

    public ICollection<ComponentProductionOrder> ProductionOrders { get; set; } =
        new List<ComponentProductionOrder>();

    public ICollection<ResearchOrder> ResearchOrders { get; set; } =
        new List<ResearchOrder>();

    public ICollection<ShipAssemblyOrder> AssemblyOrders { get; set; } =
        new List<ShipAssemblyOrder>();

    public ICollection<Ship> Ships { get; set; } =
        new List<Ship>();
}
