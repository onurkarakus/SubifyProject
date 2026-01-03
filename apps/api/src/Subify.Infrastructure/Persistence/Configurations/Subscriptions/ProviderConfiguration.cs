using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Subscriptions;

namespace Subify.Infrastructure.Persistence.Configurations.Subscriptions;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LogoUrl).HasMaxLength(500);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");
        builder.Property(p => p.Price);
        builder.Property(p => p.PriceBefore);
        builder.Property(p => p.BillingCycle).IsRequired().HasMaxLength(50).HasDefaultValue("Monthly");
        builder.Property(p => p.Region).IsRequired().HasMaxLength(100).HasDefaultValue("Global");
        builder.Property(p => p.SourceUrl).HasMaxLength(500);
        builder.Property(p => p.LastVerifiedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.DeletedAt);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("IX_Providers_Slug_Unique");
        builder.HasIndex(p => p.IsActive).HasDatabaseName("IX_Providers_IsActive");
    }
}
