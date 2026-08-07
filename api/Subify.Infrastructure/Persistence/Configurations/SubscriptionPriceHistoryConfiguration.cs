using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPriceHistoryConfiguration : IEntityTypeConfiguration<SubscriptionPriceHistory>
{
    public void Configure(EntityTypeBuilder<SubscriptionPriceHistory> builder)
    {
        builder.ToTable("SubscriptionPriceHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OldPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.NewPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.OldCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.NewCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.ChangedAt });
        builder.HasIndex(x => x.UserId);

        // FK only — no navigation (avoids soft-delete filter warning on Subscription).
        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
