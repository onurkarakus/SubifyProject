using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.AuditLogs;
using Subify.Domain.Enums;

namespace Subify.Infrastructure.Persistence.Configurations.AuditLogs;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(nl => nl.Id);

        builder.Property(nl => nl.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(nl => nl.UserId);
        builder.Property(nl => nl.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(nl => nl.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(nl => nl.Title).IsRequired().HasMaxLength(200);
        builder.Property(nl => nl.Body).HasMaxLength(2000);
        builder.Property(nl => nl.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NotificationStatus.Sent);
        builder.Property(nl => nl.ErrorMessage).HasMaxLength(1000);
        builder.Property(nl => nl.SentAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(nl => nl.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(nl => nl.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(nl => nl.User).WithMany(u => u.NotificationLogs).HasForeignKey(nl => nl.UserId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
        builder.HasOne(nl => nl.Subscription).WithMany(s => s.NotificationLogs).HasForeignKey(nl => nl.SubscriptionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(nl => new { nl.UserId, nl.SentAt }).HasDatabaseName("IX_NotificationLogs_UserId_SentAt");
        builder.HasIndex(nl => new { nl.UserId, nl.SubscriptionId, nl.Type, nl.SentAt }).HasDatabaseName("IX_NotificationLogs_DuplicateCheck");
    }
}