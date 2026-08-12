namespace Galaxy.Domain.Entities;

public class ResearchOrder
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid PlanetId { get; set; }

    public Planet Planet { get; set; } = null!;

    public int StreamNumber { get; set; }

    public TechnologyType Technology { get; set; }

    public int TargetLevel { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime CompletesAt { get; set; }
}
