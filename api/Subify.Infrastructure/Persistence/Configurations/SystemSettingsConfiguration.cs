using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings");

        builder.Property(s => s.InstanceName)
            .HasMaxLength(SystemSettings.InstanceNameMaxLength);

        builder.Property(s => s.DefaultLocale)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.LocaleMaxLength)
            .HasDefaultValue(SupportedLocales.Default);

        builder.Property(s => s.DefaultCurrency)
            .IsRequired()
            .HasMaxLength(UserProfileConstants.MainCurrencyMaxLength)
            .HasDefaultValue(SupportedCurrencies.Default);

        builder.Property(s => s.TimeZoneId)
            .HasMaxLength(SystemSettings.TimeZoneIdMaxLength);

        builder.Property(s => s.AllowPublicRegistration)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.IsSetupComplete)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.AiProvider)
            .HasMaxLength(SystemSettings.AiProviderMaxLength);

        builder.Property(s => s.AiApiKey)
            .HasColumnName("AiApiKey");

        builder.Property(s => s.AiModel)
            .HasMaxLength(SystemSettings.AiModelMaxLength);

        builder.Property(s => s.SmtpEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.SmtpHost)
            .HasMaxLength(SystemSettings.SmtpHostMaxLength);

        builder.Property(s => s.SmtpUser)
            .HasMaxLength(SystemSettings.SmtpUserMaxLength);

        builder.Property(s => s.SmtpFromName)
            .HasMaxLength(SystemSettings.SmtpFromNameMaxLength);

        builder.Property(s => s.SmtpFromEmail)
            .HasMaxLength(SystemSettings.SmtpFromEmailMaxLength);

        // Computed helpers — not mapped
        builder.Ignore(s => s.HasAiConfigured);
        builder.Ignore(s => s.HasSmtpConfigured);
    }
}
