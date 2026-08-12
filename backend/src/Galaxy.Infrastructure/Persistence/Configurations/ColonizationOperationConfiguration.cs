using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ColonizationOperationConfiguration :
    IEntityTypeConfiguration<ColonizationOperation>
{
    public void Configure(
        EntityTypeBuilder<ColonizationOperation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShipName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BlueprintName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.BlueprintVersion)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletesAt)
            .IsRequired();

        builder.HasOne(x => x.Player)
            .WithMany(x => x.ColonizationOperations)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TargetPlanet)
            .WithMany()
            .HasForeignKey(x => x.TargetPlanetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => x.TargetPlanetId).IsUnique();
        builder.HasIndex(x => x.ConsumedShipId).IsUnique();
    }
}
