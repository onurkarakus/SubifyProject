using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.AuditLogs;

namespace Subify.Infrastructure.Persistence.Configurations.AuditLogs;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(al => al.Id);
        builder.Property(al => al.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(al => al.UserId);
        builder.Property(al => al.EntityType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(al => al.EntityId);
        builder.Property(al => al.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(al => al.Description).IsRequired().HasMaxLength(1000);
        builder.Property(al => al.OldValues);
        builder.Property(al => al.NewValues);
        builder.Property(al => al.IpAddress).HasMaxLength(45);
        builder.Property(al => al.UserAgent).HasMaxLength(500);
        builder.Property(al => al.Details).HasMaxLength(2000);
        builder.Property(al => al.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(al => al.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.HasOne(al => al.User)
               .WithMany(u => u.ActivityLogs)
               .HasForeignKey(al => al.UserId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);
        builder.HasIndex(al => new { al.UserId, al.CreatedAt })
               .HasDatabaseName("IX_ActivityLogs_UserId_CreatedAt");
    }
}
