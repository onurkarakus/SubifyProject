using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class EmailTemplatesConfiguration : IEntityTypeConfiguration<EmailTemplates>
{
    public void Configure(EntityTypeBuilder<EmailTemplates> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Body)
            .IsRequired();

        builder.HasIndex(t => new { t.Name, t.LanguageCode })
            .IsUnique();
    }
}
