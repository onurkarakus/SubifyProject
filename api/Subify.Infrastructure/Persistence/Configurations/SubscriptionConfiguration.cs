using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subify.Domain.Constants;
using Subify.Domain.Entities;

namespace Subify.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(SubscriptionConstants.NameMaxLength);

        builder.Property(s => s.Price)
            .HasPrecision(SubscriptionConstants.PricePrecision, SubscriptionConstants.PriceScale);

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(SubscriptionConstants.CurrencyMaxLength);

        builder.Property(s => s.BillingCycle)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.SharedWithCount)
            .IsRequired()
            .HasDefaultValue(SubscriptionConstants.MinSharedWithCount);

        builder.Property(s => s.Notes)
            .HasMaxLength(SubscriptionConstants.NotesMaxLength);

        builder.Property(s => s.ProviderId)
            .IsRequired(false);

        // UserShare / monthly/yearly equivalents are computed — not mapped
        builder.Ignore(s => s.UserShare);
        builder.Ignore(s => s.MonthlyEquivalentShare);
        builder.Ignore(s => s.YearlyEquivalentShare);
        builder.Ignore(s => s.IsActive);

        builder.HasIndex(s => new { s.UserId, s.Archived, s.NextRenewalDate });

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Provider)
            .WithMany()
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.UserCategory)
            .WithMany()
            .HasForeignKey(s => s.UserCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
