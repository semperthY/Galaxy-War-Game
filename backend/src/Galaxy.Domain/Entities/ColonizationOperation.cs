namespace Galaxy.Domain.Entities;

public class ColonizationOperation
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid SourcePlanetId { get; set; }

    public Guid TargetPlanetId { get; set; }

    public Planet TargetPlanet { get; set; } = null!;

    public Guid ConsumedShipId { get; set; }

    public string ShipName { get; set; } = null!;

    public string BlueprintName { get; set; } = null!;

    public int BlueprintVersion { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime CompletesAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
