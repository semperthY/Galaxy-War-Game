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

        builder.HasOne(x => x.Player)
            .WithMany(x => x.Planets)
            .HasForeignKey(x => x.PlayerId);

        builder.HasOne(x => x.StarSystem)
            .WithMany(x => x.Planets)
            .HasForeignKey(x => x.StarSystemId);
    }
}
