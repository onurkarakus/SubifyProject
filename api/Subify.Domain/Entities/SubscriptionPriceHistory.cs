using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Soft audit of subscription price/currency changes (task 16.4.1).
/// Written when Update changes Price or Currency.
/// </summary>
public class SubscriptionPriceHistory : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; }

    public decimal OldPrice { get; private set; }
    public string OldCurrency { get; private set; } = null!;
    public decimal NewPrice { get; private set; }
    public string NewCurrency { get; private set; } = null!;

    public DateTimeOffset ChangedAt { get; private set; }

    /// <summary>True when same currency and new price is higher.</summary>
    public bool IsIncrease =>
        string.Equals(OldCurrency, NewCurrency, StringComparison.OrdinalIgnoreCase)
        && NewPrice > OldPrice;

    /// <summary>True when same currency and new price is lower.</summary>
    public bool IsDecrease =>
        string.Equals(OldCurrency, NewCurrency, StringComparison.OrdinalIgnoreCase)
        && NewPrice < OldPrice;

    protected SubscriptionPriceHistory()
    {
    }

    public static SubscriptionPriceHistory Create(
        Guid subscriptionId,
        Guid userId,
        decimal oldPrice,
        string oldCurrency,
        decimal newPrice,
        string newCurrency,
        DateTimeOffset? changedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(newCurrency);

        var at = changedAt ?? DateTimeOffset.UtcNow;
        return new SubscriptionPriceHistory
        {
            Id = GuidGenerator.NewId(),
            SubscriptionId = subscriptionId,
            UserId = userId,
            OldPrice = decimal.Round(oldPrice, 2, MidpointRounding.AwayFromZero),
            OldCurrency = oldCurrency.Trim().ToUpperInvariant(),
            NewPrice = decimal.Round(newPrice, 2, MidpointRounding.AwayFromZero),
            NewCurrency = newCurrency.Trim().ToUpperInvariant(),
            ChangedAt = at,
            CreatedAt = at
        };
    }
}
