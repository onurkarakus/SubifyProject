using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Infrastructure.Persistence.Configurations.Users;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Email).IsRequired().HasMaxLength(320);
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.FullName).HasMaxLength(200);
        builder.Property(p => p.Locale).IsRequired().HasMaxLength(5).HasDefaultValue("tr");
        builder.Property(p => p.ApplicationThemeColor).IsRequired().HasMaxLength(50).HasDefaultValue("Royal Purple");
        builder.Property(p => p.DarkTheme).HasDefaultValue(false);
        builder.Property(p => p.MainCurrency).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");
        builder.Property(p => p.MonthlyBudget).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(p => p.Plan).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PlanType.Free);
        builder.Property(p => p.PlanRenewsAt);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(p => p.User)
               .WithOne(u => u.Profile)
               .HasForeignKey<Profile>(p => p.Id)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);

        builder.HasOne(p => p.NotificationSettings)
                .WithOne(ns => ns.Profile)
                .HasForeignKey<NotificationSetting>(ns => ns.Id)
                .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p =>p.Plan).HasDatabaseName("IX_Profiles_Plan");
    }
}
