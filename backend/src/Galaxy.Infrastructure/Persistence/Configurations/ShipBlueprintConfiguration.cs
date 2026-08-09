using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ShipBlueprintConfiguration :
    IEntityTypeConfiguration<ShipBlueprint>
{
    public void Configure(
        EntityTypeBuilder<ShipBlueprint> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.HullCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PlayerId,
            x.Name,
            x.Version
        }).IsUnique();

        builder.HasOne(x => x.Player)
            .WithMany(x => x.Blueprints)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
