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

    public DateTime ResourcesUpdatedAt { get; set; }

    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }

    public Guid StarSystemId { get; set; }

    public StarSystem StarSystem { get; set; } = null!;
}
