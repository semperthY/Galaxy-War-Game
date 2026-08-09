using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class PlanetConfiguration : IEntityTypeConfiguration<Planet>
{
    public void Configure(EntityTypeBuilder<Planet> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Position)
            .IsRequired();

        builder.Property(x => x.Materials)
            .HasPrecision(20, 4)
            .IsRequired();

        builder.Property(x => x.Deuterium)
            .HasPrecision(20, 4)
            .IsRequired();

        builder.Property(x => x.MaterialsExtractorLevel)
            .IsRequired();

        builder.Property(x => x.DeuteriumExtractorLevel)
            .IsRequired();

        builder.Property(x => x.PowerPlantLevel)
            .IsRequired();

        builder.Property(x => x.WarehouseLevel)
            .IsRequired();

        builder.Property(x => x.BuildingSiteCapacity)
            .IsRequired();

        builder.Property(x => x.ResourcesUpdatedAt)
            .IsRequired();

        builder.Property(x => x.QueuedBuilding);

        builder.Property(x => x.QueuedBuildingLevel);

        builder.Property(x => x.BuildingCompletesAt);

        builder.HasOne(x => x.Player)
            .WithMany(x => x.Planets)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.StarSystem)
            .WithMany(x => x.Planets)
            .HasForeignKey(x => x.StarSystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.StarSystemId,
            x.Position
        }).IsUnique();
    }
}
