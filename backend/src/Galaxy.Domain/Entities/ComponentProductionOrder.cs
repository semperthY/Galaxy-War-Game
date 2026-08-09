namespace Galaxy.Domain.Entities;

public class ComponentProductionOrder
{
    public Guid Id { get; set; }

    public Guid PlanetId { get; set; }

    public Planet Planet { get; set; } = null!;

    public int LineNumber { get; set; }

    public int QueuePosition { get; set; }

    public string ComponentCode { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletesAt { get; set; }
}
