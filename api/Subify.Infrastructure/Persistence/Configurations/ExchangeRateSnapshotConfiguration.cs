using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.ToTable("ExchangeRateSnapshots");

        builder.Property(e => e.BaseCurrency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.TargetCurrency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.FetchedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.BaseCurrency, e.TargetCurrency, e.FetchedAt });
    }
}
