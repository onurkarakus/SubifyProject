using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.ApplicationPayments;
using Subify.Domain.Enums;

namespace Subify.Infrastructure.Persistence.Configurations.ApplicationPayments;

public sealed class BillingSessionConfiguration : IEntityTypeConfiguration<BillingSession>
{
    public void Configure(EntityTypeBuilder<BillingSession> builder)
    {
        builder.ToTable("BillingSessions");

        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(bs => bs.UserId).IsRequired();
        builder.Property(bs => bs.Provider).IsRequired().HasMaxLength(30).HasDefaultValue("revenuecat");
        builder.Property(bs => bs.SessionId).IsRequired().HasMaxLength(200);
        builder.Property(bs => bs.Plan).IsRequired().HasMaxLength(30);
        builder.Property(bs => bs.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(BillingSessionStatus.Pending);
        builder.Property(bs => bs.CheckoutUrl).HasMaxLength(1000);
        builder.Property(bs => bs.ExpiresAt);
        builder.Property(bs => bs.CompletedAt);
        builder.Property(bs => bs.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(bs => bs.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(bs => bs.User).WithMany(u => u.BillingSessions).HasForeignKey(bs => bs.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasIndex(bs => new { bs.Provider, bs.SessionId }).IsUnique().HasDatabaseName("IX_BillingSessions_Provider_SessionId_Unique");
        builder.HasIndex(bs => new { bs.UserId, bs.Status, bs.CreatedAt }).HasDatabaseName("IX_BillingSessions_UserId_Status_CreatedAt");
    }
}