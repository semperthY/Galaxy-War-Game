namespace Galaxy.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public ICollection<Planet> Planets { get; set; } = new List<Planet>();
}
