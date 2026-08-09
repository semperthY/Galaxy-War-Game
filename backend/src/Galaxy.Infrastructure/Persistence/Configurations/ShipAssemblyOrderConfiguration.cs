using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ShipAssemblyOrderConfiguration :
    IEntityTypeConfiguration<ShipAssemblyOrder>
{
    public void Configure(
        EntityTypeBuilder<ShipAssemblyOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QueuePosition)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PlanetId,
            x.QueuePosition
        }).IsUnique();

        builder.HasOne(x => x.Planet)
            .WithMany(x => x.AssemblyOrders)
            .HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Blueprint)
            .WithMany()
            .HasForeignKey(x => x.ShipBlueprintId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
