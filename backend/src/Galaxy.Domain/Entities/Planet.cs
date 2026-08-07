namespace Galaxy.Domain.Entities;

public class Planet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public decimal Metal { get; set; }

    public decimal Crystal { get; set; }

    public decimal Deuterium { get; set; }

    public int MetalMineLevel { get; set; }

    public int CrystalMineLevel { get; set; }

    public int DeuteriumMineLevel { get; set; }

    public DateTime ResourcesUpdatedAt { get; set; }

    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }

    public Guid StarSystemId { get; set; }

    public StarSystem StarSystem { get; set; } = null!;
}
