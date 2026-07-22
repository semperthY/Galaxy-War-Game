namespace Galaxy.Domain.Entities;

public class Planet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public Guid StarSystemId { get; set; }

    public StarSystem StarSystem { get; set; } = null!;
}
