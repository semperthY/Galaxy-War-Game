using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public sealed class FleetConfiguration : IEntityTypeConfiguration<Fleet>
{
    public void Configure(EntityTypeBuilder<Fleet> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MaterialsCargo).HasPrecision(20, 4);
        builder.Property(x => x.DeuteriumCargo).HasPrecision(20, 4);
        builder.Property(x => x.FuelReserve).HasPrecision(20, 4);
        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => new { x.GalaxyNumber, x.SystemNumber, x.Position });
        builder.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Planet>().WithMany().HasForeignKey(x => x.HomePlanetId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PirateCell>().WithMany().HasForeignKey(x => x.PirateCellId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FleetShipConfiguration : IEntityTypeConfiguration<FleetShip>
{
    public void Configure(EntityTypeBuilder<FleetShip> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BlueprintName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ComponentCodesJson).HasColumnType("jsonb");
        foreach (var property in new[]
        {
            nameof(FleetShip.LocalSpeed), nameof(FleetShip.InterSystemSpeed),
            nameof(FleetShip.CargoCapacity), nameof(FleetShip.MiningRatePerMinute),
            nameof(FleetShip.ScanRange), nameof(FleetShip.MaxHull),
            nameof(FleetShip.Hull), nameof(FleetShip.MaxShield),
            nameof(FleetShip.Shield), nameof(FleetShip.LaserShieldDamage),
            nameof(FleetShip.LaserHullDamage), nameof(FleetShip.MissileShieldDamage),
            nameof(FleetShip.MissileHullDamage), nameof(FleetShip.ComponentMaterials),
            nameof(FleetShip.ComponentDeuterium)
        }) builder.Property<decimal>(property).HasPrecision(20, 4);
        builder.HasOne(x => x.Fleet).WithMany(x => x.Ships)
            .HasForeignKey(x => x.FleetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Ship).WithOne(x => x.FleetShip)
            .HasForeignKey<FleetShip>(x => x.ShipId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ShipId).IsUnique();
    }
}

public sealed class FlightCommandConfiguration : IEntityTypeConfiguration<FlightCommand>
{
    public void Configure(EntityTypeBuilder<FlightCommand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ManifestMaterials).HasPrecision(20, 4);
        builder.Property(x => x.ManifestDeuterium).HasPrecision(20, 4);
        builder.Property(x => x.Outcome).HasMaxLength(400);
        builder.HasOne(x => x.Fleet).WithMany(x => x.Commands)
            .HasForeignKey(x => x.FleetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.FleetId, x.Sequence }).IsUnique();
    }
}

public sealed class ResourceFieldConfiguration : IEntityTypeConfiguration<ResourceField>
{
    public void Configure(EntityTypeBuilder<ResourceField> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        DecimalProperties(builder, nameof(ResourceField.Materials), nameof(ResourceField.Deuterium),
            nameof(ResourceField.MaxMaterials), nameof(ResourceField.MaxDeuterium),
            nameof(ResourceField.RegenPerHour), nameof(ResourceField.ThroughputPerHour));
        builder.HasOne<StarSystem>().WithMany().HasForeignKey(x => x.StarSystemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.StarSystemId, x.Position }).IsUnique();
    }

    internal static void DecimalProperties<TEntity>(EntityTypeBuilder<TEntity> builder, params string[] names)
        where TEntity : class
    {
        foreach (var name in names) builder.Property<decimal>(name).HasPrecision(20, 4);
    }
}

public sealed class DebrisFieldConfiguration : IEntityTypeConfiguration<DebrisField>
{
    public void Configure(EntityTypeBuilder<DebrisField> builder)
    {
        builder.HasKey(x => x.Id);
        ResourceFieldConfiguration.DecimalProperties(builder,
            nameof(DebrisField.Materials), nameof(DebrisField.Deuterium));
        builder.Property(x => x.ComponentsJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.GalaxyNumber, x.SystemNumber, x.Position });
    }
}

public sealed class PirateCellConfiguration : IEntityTypeConfiguration<PirateCell>
{
    public void Configure(EntityTypeBuilder<PirateCell> builder)
    {
        builder.HasKey(x => x.Id);
        ResourceFieldConfiguration.DecimalProperties(builder,
            nameof(PirateCell.Materials), nameof(PirateCell.Deuterium));
        builder.HasOne<StarSystem>().WithMany().HasForeignKey(x => x.StarSystemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.StarSystemId).IsUnique();
    }
}

public sealed class BattleConfiguration : IEntityTypeConfiguration<Battle>
{
    public void Configure(EntityTypeBuilder<Battle> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.AttackerFleetId);
        builder.HasIndex(x => x.DefenderFleetId);
    }
}

public sealed class BattleOrderConfiguration : IEntityTypeConfiguration<BattleOrder>
{
    public void Configure(EntityTypeBuilder<BattleOrder> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetPriority).HasMaxLength(32).IsRequired();
        builder.HasOne<Battle>().WithMany().HasForeignKey(x => x.BattleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BattleId, x.FleetId, x.Round }).IsUnique();
    }
}

public sealed class ShipServiceOrderConfiguration : IEntityTypeConfiguration<ShipServiceOrder>
{
    public void Configure(EntityTypeBuilder<ShipServiceOrder> builder)
    {
        builder.HasKey(x => x.Id);
        ResourceFieldConfiguration.DecimalProperties(builder,
            nameof(ShipServiceOrder.MaterialsCost), nameof(ShipServiceOrder.DeuteriumCost));
        builder.HasOne<FleetShip>().WithMany().HasForeignKey(x => x.FleetShipId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Planet>().WithMany().HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.FleetShipId).IsUnique();
    }
}
