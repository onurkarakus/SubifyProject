using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.Property(r => r.PageName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.LanguageCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(r => r.Value)
            .IsRequired();

        builder.HasIndex(r => new { r.PageName, r.Name, r.LanguageCode })
            .IsUnique();
    }
}
