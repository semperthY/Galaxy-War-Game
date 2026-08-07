namespace Galaxy.Domain.Entities;

public class StarSystem
{
    public Guid Id { get; set; }

    public int GalaxyNumber { get; set; }

    public int SystemNumber { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<Planet> Planets { get; set; } = new List<Planet>();
}
