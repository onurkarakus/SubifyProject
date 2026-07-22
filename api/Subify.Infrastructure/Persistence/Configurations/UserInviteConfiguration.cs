using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class UserInviteConfiguration : IEntityTypeConfiguration<UserInvite>
{
    public void Configure(EntityTypeBuilder<UserInvite> builder)
    {
        builder.ToTable("UserInvites");

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(UserInvite.EmailMaxLength);

        builder.Property(i => i.TokenHash)
            .IsRequired()
            .HasMaxLength(UserInvite.TokenHashMaxLength);

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.Property(i => i.CreatedByUserId)
            .IsRequired();

        // Computed helpers
        builder.Ignore(i => i.IsUsed);

        builder.HasIndex(i => i.TokenHash)
            .IsUnique();

        builder.HasIndex(i => new { i.Email, i.UsedAt });

        builder.HasIndex(i => i.CreatedByUserId);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AcceptedUser)
            .WithMany()
            .HasForeignKey(i => i.AcceptedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
