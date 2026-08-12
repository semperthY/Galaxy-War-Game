namespace Galaxy.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public RaceType Race { get; set; }

    public TechnologyType? QueuedTechnology { get; set; }

    public int? QueuedTechnologyLevel { get; set; }

    public DateTime? ResearchCompletesAt { get; set; }

    public ICollection<Planet> Planets { get; set; } =
        new List<Planet>();

    public ICollection<PlayerTechnology> Technologies { get; set; } =
        new List<PlayerTechnology>();

    public ICollection<ShipBlueprint> Blueprints { get; set; } =
        new List<ShipBlueprint>();

    public ICollection<Ship> Ships { get; set; } =
        new List<Ship>();

    public ICollection<ColonizationOperation> ColonizationOperations { get; set; } =
        new List<ColonizationOperation>();
}
