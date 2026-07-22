using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.CreatedByIp)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(t => t.ReasonRevoked)
            .HasMaxLength(100);

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.Property(t => t.DeviceId)
            .HasMaxLength(200);

        builder.Property(t => t.UserAgent)
            .HasMaxLength(500);

        // Computed helpers
        builder.Ignore(t => t.IsRevoked);

        builder.HasIndex(t => t.TokenHash);
        builder.HasIndex(t => new { t.UserId, t.RevokedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
