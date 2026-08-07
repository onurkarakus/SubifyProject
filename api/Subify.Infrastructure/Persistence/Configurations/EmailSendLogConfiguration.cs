using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class EmailSendLogConfiguration : IEntityTypeConfiguration<EmailSendLog>
{
    public void Configure(EntityTypeBuilder<EmailSendLog> builder)
    {
        builder.ToTable("EmailSendLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TemplateName)
            .HasMaxLength(EmailSendLog.TemplateNameMaxLength)
            .IsRequired();

        builder.Property(e => e.ToEmail)
            .HasMaxLength(EmailSendLog.ToEmailMaxLength)
            .IsRequired();

        builder.Property(e => e.DedupeKey)
            .HasMaxLength(EmailSendLog.DedupeKeyMaxLength);

        builder.Property(e => e.Error)
            .HasMaxLength(EmailSendLog.ErrorMaxLength);

        // Successful sends only: one row per dedupe key
        builder.HasIndex(e => e.DedupeKey)
            .IsUnique()
            .HasFilter("\"DedupeKey\" IS NOT NULL AND \"Success\" = TRUE");

        builder.HasIndex(e => new { e.TemplateName, e.SentAt });
        builder.HasIndex(e => e.UserId);
    }
}
