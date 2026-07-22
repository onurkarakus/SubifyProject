using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Task 2.2.9: keep ASP.NET Identity default table name (PascalCase, not snake_case).
        builder.ToTable("AspNetUsers");

        // No freemium plan / plan_renews_at columns (Subify OS).

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.FullNameMaxLength);

        builder.Property(u => u.Locale)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.LocaleMaxLength)
            .HasDefaultValue(SupportedLocales.Default);

        builder.Property(u => u.MainCurrency)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.MainCurrencyMaxLength)
            .HasDefaultValue(SupportedCurrencies.Default);

        builder.Property(u => u.MonthlyBudget)
            .HasPrecision(
                UserProfileConstants.MonthlyBudgetPrecision,
                UserProfileConstants.MonthlyBudgetScale);

        builder.Property(u => u.ApplicationThemeColor)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.ThemeColorMaxLength)
            .HasDefaultValue(ThemeColors.Default);

        builder.Property(u => u.DarkTheme)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);
    }
}
