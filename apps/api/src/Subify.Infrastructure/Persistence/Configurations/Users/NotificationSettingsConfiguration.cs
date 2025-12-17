using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Infrastructure.Persistence.Configurations.Users;

/// <summary>
/// EF Core configuration for NotificationSettings entity.
/// Uses shared primary key pattern with Profile (NotificationSettings.Id = Profile.Id).
/// </summary>
public sealed class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.ToTable("NotificationSettings");

        builder.HasKey(ns => ns.Id);

        // Shared primary key: NotificationSettings.Id = Profile.Id
        builder.Property(ns => ns.Id)
            .ValueGeneratedNever();

        builder.Property(ns => ns.EmailEnabled)
            .HasDefaultValue(true);

        builder.Property(ns => ns.PushEnabled)
            .HasDefaultValue(false);

        builder.Property(ns => ns.DaysBeforeRenewal)
            .HasDefaultValue(3);

        builder.Property(ns => ns.CreatedAt)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(ns => ns.UpdatedAt)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Relationship is configured in ProfileConfiguration
    }
}