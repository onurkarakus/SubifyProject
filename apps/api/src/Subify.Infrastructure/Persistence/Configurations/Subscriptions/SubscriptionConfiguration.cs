using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.Subscriptions;
using Subify.Domain.Enums;

namespace Subify.Infrastructure.Persistence.Configurations.Subscriptions;

/// <summary>
/// EF Core configuration for Subscription entity.
/// This is the core entity of the application.
/// </summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Price).HasPrecision(10, 2).IsRequired();
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");

        builder.Property(s => s.BillingCycle).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(s => s.NextRenewalDate).IsRequired();
        builder.Property(s => s.LastUsedAt);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(SubscriptionStatus.Active);

        builder.Property(s => s.Archived).HasDefaultValue(false);
        builder.Property(s => s.SharedWithCount).HasDefaultValue(1);

        builder.Ignore(s => s.UserShare);

        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(s => s.User).WithMany(u => u.Subscriptions).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Category).WithMany(c => c.Subscriptions).HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.UserCategory).WithMany(uc => uc.Subscriptions).HasForeignKey(s => s.UserCategoryId).OnDelete(DeleteBehavior.ClientSetNull);
        builder.HasIndex(s => new { s.UserId, s.Archived, s.NextRenewalDate }).HasDatabaseName("IX_Subscriptions_UserId_Archived_NextRenewalDate");
        builder.HasIndex(s => s.DeletedAt).HasFilter("[DeletedAt] IS NULL").HasDatabaseName("IX_Subscriptions_DeletedAt_Active");
        builder.HasIndex(s => new { s.UserId, s.CategoryId }).HasDatabaseName("IX_Subscriptions_UserId_CategoryId");
        builder.HasIndex(s => new { s.NextRenewalDate, s.Archived, s.Status }).HasDatabaseName("IX_Subscriptions_NextRenewalDate_Active");
    }
}