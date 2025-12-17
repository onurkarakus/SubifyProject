using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Subscriptions;

namespace Subify.Infrastructure.Persistence.Configurations.Subscriptions;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Icon).HasMaxLength(50);
        builder.Property(c => c.Color).HasMaxLength(20);
        builder.Property(c => c.SortOrder).HasDefaultValue(0);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("IX_Categories_Slug_Unique");
        builder.HasIndex(c => new { c.IsActive, c.SortOrder }).HasDatabaseName("IX_Categories_IsActive_SortOrder");
    }
}
