namespace Galaxy.Domain.Entities;

public class Planet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public long Metal { get; set; }

    public long Crystal { get; set; }

    public long Deuterium { get; set; }

    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }

    public Guid StarSystemId { get; set; }

    public StarSystem StarSystem { get; set; } = null!;
}
