using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities.Users;

namespace Subify.Infrastructure.Persistence.Configurations.Users;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.HasIndex(u => u.DeletedAt)
            .HasFilter("[DeletedAt] IS NULL")
            .HasDatabaseName("IX_AspNetUsers_DeletedAt_Active");

        builder.HasIndex(u => new { u.IsActive, u.Email })
            .HasDatabaseName("IX_AspNetUsers_IsActive_Email");       
    }
}
