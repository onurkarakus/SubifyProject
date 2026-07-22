using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("UserDeviceTokens");

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(UserDeviceToken.TokenMaxLength);

        builder.Property(t => t.Platform)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.DeviceName)
            .HasMaxLength(UserDeviceToken.DeviceNameMaxLength);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Unique token string across the instance (same FCM token shouldn't bind two users)
        builder.HasIndex(t => t.Token)
            .IsUnique();

        builder.HasIndex(t => new { t.UserId, t.IsActive });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
