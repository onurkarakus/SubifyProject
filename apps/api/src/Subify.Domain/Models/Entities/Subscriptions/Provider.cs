using Subify.Domain.Abstractions.Common;
using Subify.Domain.Models.Entities.Common;

namespace Subify.Domain.Models.Entities.Subscriptions;

/// <summary>
/// Represents a subscription provider with details such as name, pricing, region, and other metadata.
/// </summary>
public class Provider : BaseEntity, ISoftDeletable
{
    /// <summary>
    /// Gets or sets the name of the provider.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique slug identifier for the provider.
    /// </summary>
    public string Slug { get; set; }

    /// <summary>
    /// Gets or sets the URL of the provider's logo.
    /// </summary>
    public string LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the currency used by the provider.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Gets or sets the current price of the subscription.
    /// </summary>
    public float? Price { get; set; }

    /// <summary>
    /// Gets or sets the price of the subscription before any discounts.
    /// </summary>
    public float? PriceBefore { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle of the subscription (e.g., monthly, yearly).
    /// </summary>
    public string BillingCycle { get; set; }

    /// <summary>
    /// Gets or sets the region where the provider operates.
    /// </summary>
    public string Region { get; set; }

    /// <summary>
    /// Gets or sets the source URL for additional information about the provider.
    /// </summary>
    public string SourceUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the provider's details were last verified.
    /// </summary>
    public DateTimeOffset LastVerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the provider was soft-deleted. Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
