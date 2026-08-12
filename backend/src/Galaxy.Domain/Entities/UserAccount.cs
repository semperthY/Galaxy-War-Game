namespace Galaxy.Domain.Entities;

public class UserAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string CommanderName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }
}
