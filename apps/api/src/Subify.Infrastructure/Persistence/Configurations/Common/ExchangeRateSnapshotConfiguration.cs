using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Models.Entities.Common;

namespace Subify.Infrastructure.Persistence.Configurations.Common;

public sealed class ExchangeRateSnapshotConfiguration : IEntityTypeConfiguration<ExchangeRateSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeRateSnapshot> builder)
    {
        builder.ToTable("ExchangeRateSnapshots");

        builder.HasKey(ers => ers.Id);

        builder.Property(ers => ers.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(ers => ers.BaseCurrency).IsRequired().HasMaxLength(10);
        builder.Property(ers => ers.Rates).IsRequired();
        builder.Property(ers => ers.Source).IsRequired().HasMaxLength(100).HasDefaultValue("exchangerate-api. com");
        builder.Property(ers => ers.FetchedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(ers => ers.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(ers => ers.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(ers => new { ers.BaseCurrency, ers.FetchedAt }).HasDatabaseName("IX_ExchangeRateSnapshots_BaseCurrency_FetchedAt");
    }
}