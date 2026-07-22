using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.ToTable("NotificationSettings");

        // OS default: email off until SMTP + EmailSend (Faz 15) — task 3.2.11
        builder.Property(n => n.EmailEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.PushEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.DaysBeforeRenewal)
            .IsRequired()
            .HasDefaultValue(3);

        // One settings row per user
        builder.HasIndex(n => n.UserId)
            .IsUnique();

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
