using Microsoft.EntityFrameworkCore;
using Subify.Domain.Entities.Auth;

namespace Subify.Infrastructure.Persistence.Configurations.Auth;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(rt => rt.UserId).IsRequired();
        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(256);
        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.IsRevoked).HasDefaultValue(false);
        builder.Property(rt => rt.RevokedReason).HasMaxLength(200);
        builder.Property(rt => rt.DeviceId).HasMaxLength(100);
        builder.Property(rt => rt.IpAddress).HasMaxLength(45);
        builder.Property(rt => rt.UserAgent).HasMaxLength(500);
        builder.Property(rt => rt.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(rt => rt.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rt => rt.TokenHash).HasDatabaseName("IX_RefreshTokens_TokenHash");
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt }).HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked_ExpiresAt");
    }
}
