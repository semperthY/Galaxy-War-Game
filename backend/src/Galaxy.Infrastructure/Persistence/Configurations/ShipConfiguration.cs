using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ShipConfiguration :
    IEntityTypeConfiguration<Ship>
{
    public void Configure(EntityTypeBuilder<Ship> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Player)
            .WithMany(x => x.Ships)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Planet)
            .WithMany(x => x.Ships)
            .HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Blueprint)
            .WithMany()
            .HasForeignKey(x => x.ShipBlueprintId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => x.PlanetId);
    }
}
