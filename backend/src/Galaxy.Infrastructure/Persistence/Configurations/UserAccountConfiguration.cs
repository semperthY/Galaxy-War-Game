using Galaxy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Galaxy.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(x => x.CommanderName)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.CommanderName)
            .IsUnique();

        builder.HasIndex(x => x.PlayerId)
            .IsUnique();

        builder.HasOne(x => x.Player)
            .WithOne()
            .HasForeignKey<UserAccount>(x => x.PlayerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
