using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ComponentInventoryItemConfiguration :
    IEntityTypeConfiguration<ComponentInventoryItem>
{
    public void Configure(
        EntityTypeBuilder<ComponentInventoryItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PlanetId,
            x.ComponentCode
        }).IsUnique();

        builder.HasOne(x => x.Planet)
            .WithMany(x => x.ComponentInventory)
            .HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
