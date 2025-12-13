using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.Subscriptions;

namespace Subify.Infrastructure.Persistence.Configurations.Subscriptions;

public sealed class UserCategoryConfiguration : IEntityTypeConfiguration<UserCategory>
{
    public void Configure(EntityTypeBuilder<UserCategory> builder)
    {
        builder.ToTable("UserCategories");

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(uc => uc.UserId);
        builder.Property(uc => uc.Name).IsRequired().HasMaxLength(100);
        builder.Property(uc => uc.Slug).IsRequired().HasMaxLength(100);
        builder.Property(uc => uc.Icon).HasMaxLength(50);
        builder.Property(uc => uc.Color).HasMaxLength(20);
        builder.Property(uc => uc.SortOrder).HasDefaultValue(0);
        builder.Property(uc => uc.IsActive).HasDefaultValue(true);
        builder.Property(uc => uc.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(uc => uc.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(uc => uc.User).WithMany(u => u.UserCategories).HasForeignKey(uc => uc.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasIndex(uc => new { uc.UserId, uc.Slug }).IsUnique().HasDatabaseName("IX_UserCategories_UserId_Slug_Unique");
        builder.HasIndex(uc => new { uc.UserId, uc.IsActive, uc.SortOrder }).HasDatabaseName("IX_UserCategories_UserId_IsActive_SortOrder");

    }
}