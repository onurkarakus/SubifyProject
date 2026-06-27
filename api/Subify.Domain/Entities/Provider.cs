using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities;

public class Provider : BaseEntity, ISoftDeletable
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Logout { get; private set; }
    public string Currency { get; private set; }
    public decimal? Price { get; private set; }
    public decimal? PriceBefore { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public string Region { get; private set; }
    public string? SourceUrl { get; private set; }
    public DateTimeOffset? LastVerifiedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }

    protected Provider() { }

    public void Create(string name, string slug, string? logout, string currency, decimal? price, decimal? priceBefore, BillingCycle billingCycle, string region, string? sourceUrl, DateTimeOffset? lastVerifiedAt)
    {
        Name = name;
        Slug = slug;
        Logout = logout;
        Currency = currency;
        Price = price;
        PriceBefore = priceBefore;
        BillingCycle = billingCycle;
        Region = region;
        SourceUrl = sourceUrl;
        LastVerifiedAt = lastVerifiedAt;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string slug, string? logout, string currency, decimal? price, decimal? priceBefore, BillingCycle billingCycle, string region, string? sourceUrl, DateTimeOffset? lastVerifiedAt)
    {
        Name = name;
        Slug = slug;
        Logout = logout;
        Currency = currency;
        Price = price;
        PriceBefore = priceBefore;
        BillingCycle = billingCycle;
        Region = region;
        SourceUrl = sourceUrl;
        LastVerifiedAt = lastVerifiedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
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