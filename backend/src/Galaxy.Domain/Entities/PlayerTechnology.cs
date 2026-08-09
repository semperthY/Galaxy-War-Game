namespace Galaxy.Domain.Entities;

public class PlayerTechnology
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public TechnologyType Technology { get; set; }

    public int Level { get; set; }
}
