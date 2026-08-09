using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public class PlayerTechnologyConfiguration :
    IEntityTypeConfiguration<PlayerTechnology>
{
    public void Configure(
        EntityTypeBuilder<PlayerTechnology> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Technology)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PlayerId,
            x.Technology
        }).IsUnique();

        builder.HasOne(x => x.Player)
            .WithMany(x => x.Technologies)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
