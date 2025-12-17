using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Subscriptions;

namespace Subify.Infrastructure.Persistence.Configurations.Subscriptions;

public sealed class SubscriptionPaymentRecordConfiguration : IEntityTypeConfiguration<SubscriptionPaymentRecord>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentRecord> builder)
    {
        builder.ToTable("SubscriptionPaymentRecords");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(pr => pr.SubscriptionId).IsRequired();
        builder.Property(pr => pr.UserId);
        builder.Property(pr => pr.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(pr => pr.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");
        builder.Property(pr => pr.PaymentDate).IsRequired();
        builder.Property(pr => pr.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PaymentStatus.Paid);
        builder.Property(pr => pr.PaymentMethod).HasMaxLength(50);
        builder.Property(pr => pr.Notes).HasMaxLength(500);
        builder.Property(pr => pr.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(pr => pr.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(pr => pr.Subscription).WithMany(s => s.PaymentRecords).HasForeignKey(pr => pr.SubscriptionId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(pr => pr.User).WithMany().HasForeignKey(pr => pr.UserId).OnDelete(DeleteBehavior.NoAction).IsRequired(false);
        builder.HasIndex(pr => new { pr.SubscriptionId, pr.PaymentDate }).HasDatabaseName("IX_SubscriptionPaymentRecords_SubscriptionId_PaymentDate");
        builder.HasIndex(pr => new { pr.UserId, pr.PaymentDate }).HasDatabaseName("IX_SubscriptionPaymentRecords_UserId_PaymentDate");
    }
}