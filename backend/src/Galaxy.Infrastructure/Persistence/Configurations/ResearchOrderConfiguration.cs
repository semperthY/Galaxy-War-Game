using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public sealed class ResearchOrderConfiguration :
    IEntityTypeConfiguration<ResearchOrder>
{
    public void Configure(EntityTypeBuilder<ResearchOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StreamNumber).IsRequired();
        builder.Property(x => x.Technology).IsRequired();
        builder.Property(x => x.TargetLevel).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CompletesAt).IsRequired();

        builder.HasIndex(x => new { x.PlanetId, x.StreamNumber })
            .IsUnique();
        builder.HasIndex(x => new { x.PlayerId, x.Technology })
            .IsUnique();

        builder.HasOne(x => x.Player)
            .WithMany(x => x.ResearchOrders)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Planet)
            .WithMany(x => x.ResearchOrders)
            .HasForeignKey(x => x.PlanetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
