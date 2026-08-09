using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Planet> Planets => Set<Planet>();

    public DbSet<StarSystem> StarSystems => Set<StarSystem>();

    public DbSet<PlayerTechnology> PlayerTechnologies =>
        Set<PlayerTechnology>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

