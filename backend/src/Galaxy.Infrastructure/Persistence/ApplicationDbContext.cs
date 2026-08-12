using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Planet> Planets => Set<Planet>();

    public DbSet<StarSystem> StarSystems => Set<StarSystem>();

    public DbSet<PlayerTechnology> PlayerTechnologies =>
        Set<PlayerTechnology>();

    public DbSet<ComponentInventoryItem> ComponentInventory =>
        Set<ComponentInventoryItem>();

    public DbSet<ComponentProductionOrder> ProductionOrders =>
        Set<ComponentProductionOrder>();

    public DbSet<ShipBlueprint> ShipBlueprints =>
        Set<ShipBlueprint>();

    public DbSet<ShipBlueprintModule> ShipBlueprintModules =>
        Set<ShipBlueprintModule>();

    public DbSet<ShipAssemblyOrder> ShipAssemblyOrders =>
        Set<ShipAssemblyOrder>();

    public DbSet<Ship> Ships => Set<Ship>();

    public DbSet<ColonizationOperation> ColonizationOperations =>
        Set<ColonizationOperation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
