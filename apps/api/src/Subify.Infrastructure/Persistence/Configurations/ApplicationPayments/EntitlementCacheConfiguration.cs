using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.ApplicationPayments;
using Subify.Domain.Enums;

namespace Subify.Infrastructure.Persistence.Configurations.ApplicationPayments;

public sealed class EntitlementCacheConfiguration : IEntityTypeConfiguration<EntitlementCache>
{
    public void Configure(EntityTypeBuilder<EntitlementCache> builder)
    {
        builder.ToTable("EntitlementCaches");

        builder.HasKey(ec => ec.Id);

        builder.Property(ec => ec.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(ec => ec.UserId);
        builder.Property(ec => ec.Entitlement).IsRequired().HasMaxLength(100);
        builder.Property(ec => ec.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(EntitlementStatus.Active);
        builder.Property(ec => ec.ExpiresAt);
        builder.Property(ec => ec.ProductId).HasMaxLength(100);
        builder.Property(ec => ec.Store).HasMaxLength(30);
        builder.Property(ec => ec.IsTrial).HasDefaultValue(false);
        builder.Property(ec => ec.WillRenew).HasDefaultValue(true);
        builder.Property(ec => ec.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(ec => ec.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(ec => ec.User).WithMany(u => u.Entitlements).HasForeignKey(ec => ec.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasIndex(ec => new { ec.UserId, ec.Entitlement }).IsUnique().HasDatabaseName("IX_EntitlementCaches_UserId_Entitlement_Unique");
        builder.HasIndex(ec => new { ec.UserId, ec.Status }).HasDatabaseName("IX_EntitlementCaches_UserId_Status");
    }
}