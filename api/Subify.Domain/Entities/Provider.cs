using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities;

public class Provider : BaseEntity, ISoftDeletable
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;

    /// <summary>CDN/logo image URL for the provider (was incorrectly named Logout).</summary>
    public string? LogoUrl { get; private set; }

    public string Currency { get; private set; } = null!;
    public decimal? Price { get; private set; }
    public decimal? PriceBefore { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public string Region { get; private set; } = null!;
    public string? SourceUrl { get; private set; }
    public DateTimeOffset? LastVerifiedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }

    protected Provider()
    {
    }

    /// <summary>
    /// Creates a catalog provider for seed (task 2.3.6). LogoUrl optional for self-host.
    /// </summary>
    public static Provider CreateCatalog(
        string name,
        string slug,
        string currency,
        decimal? price,
        BillingCycle billingCycle,
        string region,
        string? sourceUrl = null,
        string? logoUrl = null,
        decimal? priceBefore = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        var provider = new Provider
        {
            Id = GuidGenerator.NewId()
        };
        provider.Apply(
            name.Trim(),
            slug.Trim().ToLowerInvariant(),
            logoUrl,
            currency.Trim().ToUpperInvariant(),
            price,
            priceBefore,
            billingCycle,
            region.Trim().ToUpperInvariant(),
            sourceUrl,
            lastVerifiedAt: null);
        provider.IsActive = true;
        provider.CreatedAt = DateTimeOffset.UtcNow;
        return provider;
    }

    public void Create(
        string name,
        string slug,
        string? logoUrl,
        string currency,
        decimal? price,
        decimal? priceBefore,
        BillingCycle billingCycle,
        string region,
        string? sourceUrl,
        DateTimeOffset? lastVerifiedAt)
    {
        Apply(name, slug, logoUrl, currency, price, priceBefore, billingCycle, region, sourceUrl, lastVerifiedAt);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        string name,
        string slug,
        string? logoUrl,
        string currency,
        decimal? price,
        decimal? priceBefore,
        BillingCycle billingCycle,
        string region,
        string? sourceUrl,
        DateTimeOffset? lastVerifiedAt)
    {
        Apply(name, slug, logoUrl, currency, price, priceBefore, billingCycle, region, sourceUrl, lastVerifiedAt);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        string name,
        string slug,
        string? logoUrl,
        string currency,
        decimal? price,
        decimal? priceBefore,
        BillingCycle billingCycle,
        string region,
        string? sourceUrl,
        DateTimeOffset? lastVerifiedAt)
    {
        Name = name;
        Slug = slug;
        LogoUrl = logoUrl;
        Currency = currency;
        Price = price;
        PriceBefore = priceBefore;
        BillingCycle = billingCycle;
        Region = region;
        SourceUrl = sourceUrl;
        LastVerifiedAt = lastVerifiedAt;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
