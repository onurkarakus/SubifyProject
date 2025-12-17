using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Notifications;

namespace Subify.Infrastructure.Persistence.Configurations.Notifications;

public sealed class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
{
    public void Configure(EntityTypeBuilder<PushToken> builder)
    {
        builder.ToTable("PushTokens");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(pt => pt.UserId);
        builder.Property(pt => pt.Token).IsRequired().HasMaxLength(500);
        builder.Property(pt => pt.Platform).IsRequired().HasMaxLength(20);
        builder.Property(pt => pt.DeviceId).HasMaxLength(100);
        builder.Property(pt => pt.DeviceName).HasMaxLength(100);
        builder.Property(pt => pt.IsActive).HasDefaultValue(true);
        builder.Property(pt => pt.LastUsedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(pt => pt.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(pt => pt.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(pt => pt.User).WithMany(u => u.PushTokens).HasForeignKey(pt => pt.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasIndex(pt => new { pt.Token, pt.Platform }).IsUnique().HasDatabaseName("IX_PushTokens_Token_Platform_Unique");
        builder.HasIndex(pt => new { pt.UserId, pt.IsActive }).HasDatabaseName("IX_PushTokens_UserId_IsActive");
    }
}