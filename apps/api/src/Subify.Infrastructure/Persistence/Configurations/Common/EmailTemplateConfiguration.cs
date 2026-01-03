using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Common;

namespace Subify.Infrastructure.Persistence.Configurations.Common;

public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(et => et.Name).IsRequired().HasMaxLength(100);
        builder.Property(et => et.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(et => et.Subject).IsRequired().HasMaxLength(255);
        builder.Property(et => et.Body).IsRequired();
        builder.Property(et => et.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(et => et.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(et => new { et.Name, et.LanguageCode }).IsUnique();
    }
}
