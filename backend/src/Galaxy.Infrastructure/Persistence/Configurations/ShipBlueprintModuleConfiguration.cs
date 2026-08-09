using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class ShipBlueprintModuleConfiguration :
    IEntityTypeConfiguration<ShipBlueprintModule>
{
    public void Configure(
        EntityTypeBuilder<ShipBlueprintModule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ShipBlueprintId,
            x.ComponentCode
        }).IsUnique();

        builder.HasOne(x => x.ShipBlueprint)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.ShipBlueprintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
