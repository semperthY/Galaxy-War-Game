using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class StarSystemConfiguration : IEntityTypeConfiguration<StarSystem>
{
    public void Configure(EntityTypeBuilder<StarSystem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GalaxyNumber)
            .IsRequired();

        builder.Property(x => x.SystemNumber)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.GalaxyNumber,
            x.SystemNumber
        }).IsUnique();
    }
}
