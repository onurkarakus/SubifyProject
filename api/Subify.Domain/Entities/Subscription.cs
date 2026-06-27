using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities;

public class Subscription : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? UserCategoryId { get; private set; }

    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; }
    public int SharedWithCount { get; private set; }
    public DateOnly NextRenewalDate { get; private set; }
    public DateOnly? LastUsedAt { get; private set; }
    public string? Notes { get; private set; }
    public bool Archived { get; private set; }

    public ApplicationUser User { get; private set; } = null!;
    public Provider? Provider { get; private set; }
    public Category? Category { get; private set; }
    public UserCategory? UserCategory { get; private set; }

    public DateTimeOffset? DeletedAt { get; set; }

    protected Subscription() { }

    public void Create(Guid userId,
        Guid providerId,
        Guid? categoryId,
        Guid? userCategoryId,
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        DateOnly? lastUsedAt,
        string? notes,
        bool archived)
    {
        UserId = userId;
        ProviderId = providerId;
        CategoryId = categoryId;
        UserCategoryId = userCategoryId;
        Name = name;
        Price = price;
        Currency = currency;
        BillingCycle = billingCycle;
        SharedWithCount = sharedWithCount;
        NextRenewalDate = nextRenewalDate;
        LastUsedAt = lastUsedAt;
        Notes = notes;
        Archived = archived;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(Guid userId,
        Guid providerId,
        Guid? categoryId,
        Guid? userCategoryId,
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        DateOnly? lastUsedAt,
        string? notes,
        bool archived)
    {
        UserId = userId;
        ProviderId = providerId;
        CategoryId = categoryId;
        UserCategoryId = userCategoryId;
        Name = name;
        Price = price;
        Currency = currency;
        BillingCycle = billingCycle;
        SharedWithCount = sharedWithCount;
        NextRenewalDate = nextRenewalDate;
        LastUsedAt = lastUsedAt;
        Notes = notes;
        Archived = archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        Archived = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    public void ReActive()
    {
        Archived = false;
        DeletedAt = null;
    }
}