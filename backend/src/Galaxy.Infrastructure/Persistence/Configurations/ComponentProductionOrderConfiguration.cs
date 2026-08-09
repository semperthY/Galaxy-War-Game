using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ComponentProductionOrderConfiguration :
    IEntityTypeConfiguration<ComponentProductionOrder>
{
    public void Configure(
        EntityTypeBuilder<ComponentProductionOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.QueuePosition)
            .IsRequired();

        builder.Property(x => x.ComponentCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PlanetId,
            x.LineNumber,
            x.QueuePosition
        }).IsUnique();

        builder.HasOne(x => x.Planet)
            .WithMany(x => x.ProductionOrders)
            .HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
