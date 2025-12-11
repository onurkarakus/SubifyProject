using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.Common;

namespace Subify.Infrastructure.Persistence.Configurations.Common;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(r => r.PageName).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(r => r.Value).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(r => new { r.PageName, r.Name, r.LanguageCode }).IsUnique().HasDatabaseName("IX_Resources_PageName_Name_LanguageCode_Unique");
        builder.HasIndex(r => new { r.LanguageCode, r.IsActive, r.UpdatedAt }).HasDatabaseName("IX_Resources_LanguageCode_IsActive_UpdatedAt");
    }
}