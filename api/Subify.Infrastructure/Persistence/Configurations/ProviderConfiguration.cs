using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LogoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.Price)
            .HasPrecision(10, 2);

        builder.Property(p => p.PriceBefore)
            .HasPrecision(10, 2);

        builder.Property(p => p.BillingCycle)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.Region)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.SourceUrl)
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.HasIndex(p => p.IsActive);
    }
}
