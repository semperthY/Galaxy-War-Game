using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Race)
            .IsRequired();

        builder.Property(x => x.QueuedTechnology);

        builder.Property(x => x.QueuedTechnologyLevel);

        builder.Property(x => x.ResearchCompletesAt);

        builder.HasIndex(x => x.Username)
            .IsUnique();
    }
}

