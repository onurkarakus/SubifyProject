using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;
using Subify.Domain.Constants;
using Subify.Domain.Enums;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Domain.Entities;

/// <summary>
/// User subscription (core business entity). Soft-delete = archive.
/// </summary>
public class Subscription : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; private set; }
    public Guid? ProviderId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? UserCategoryId { get; private set; }

    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; }
    public int SharedWithCount { get; private set; }
    public DateOnly NextRenewalDate { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Soft-delete / cancel flag (archived subscriptions excluded from active totals).</summary>
    public bool Archived { get; private set; }

    public ApplicationUser User { get; private set; } = null!;
    public Provider? Provider { get; private set; }
    public Category? Category { get; private set; }
    public UserCategory? UserCategory { get; private set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>User's share of the price (Price / SharedWithCount). Not persisted.</summary>
    public decimal UserShare =>
        SharedWithCount > 0 ? decimal.Round(Price / SharedWithCount, 2, MidpointRounding.AwayFromZero) : Price;

    /// <summary>User share normalized to a monthly amount.</summary>
    public decimal MonthlyEquivalentShare => BillingCycle switch
    {
        BillingCycle.Yearly => decimal.Round(UserShare / 12m, 2, MidpointRounding.AwayFromZero),
        _ => UserShare
    };

    /// <summary>User share normalized to a yearly amount.</summary>
    public decimal YearlyEquivalentShare => BillingCycle switch
    {
        BillingCycle.Monthly => decimal.Round(UserShare * 12m, 2, MidpointRounding.AwayFromZero),
        _ => UserShare
    };

    public bool IsActive => !Archived && DeletedAt is null;

    protected Subscription()
    {
    }

    /// <summary>
    /// Creates a new subscription with domain validation.
    /// CategoryId and UserCategoryId are mutually exclusive.
    /// </summary>
    public static Result<Subscription> Create(
        Guid userId,
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        Guid? providerId = null,
        Guid? categoryId = null,
        Guid? userCategoryId = null,
        string? notes = null,
        DateOnly? today = null)
    {
        var validation = Validate(
            userId,
            name,
            price,
            currency,
            billingCycle,
            sharedWithCount,
            nextRenewalDate,
            categoryId,
            userCategoryId,
            notes,
            today);

        if (validation.IsFailure)
        {
            return Result.Failure<Subscription>(validation.Error);
        }

        var entity = new Subscription { Id = GuidGenerator.NewId() };
        entity.ApplyValues(
            userId,
            name,
            price,
            currency,
            billingCycle,
            sharedWithCount,
            nextRenewalDate,
            providerId,
            categoryId,
            userCategoryId,
            notes,
            archived: false);

        entity.CreatedAt = DateTimeOffset.UtcNow;
        return Result.Success(entity);
    }

    public Result Update(
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        Guid? providerId = null,
        Guid? categoryId = null,
        Guid? userCategoryId = null,
        string? notes = null,
        DateOnly? today = null)
    {
        var validation = Validate(
            UserId,
            name,
            price,
            currency,
            billingCycle,
            sharedWithCount,
            nextRenewalDate,
            categoryId,
            userCategoryId,
            notes,
            today,
            requireFutureRenewal: false);

        if (validation.IsFailure)
        {
            return validation;
        }

        ApplyValues(
            UserId,
            name,
            price,
            currency,
            billingCycle,
            sharedWithCount,
            nextRenewalDate,
            providerId,
            categoryId,
            userCategoryId,
            notes,
            Archived);

        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>Soft-delete: archive the subscription.</summary>
    public void Archive()
    {
        if (Archived)
        {
            return;
        }

        Archived = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Restore an archived subscription.</summary>
    public void Reactivate()
    {
        if (!Archived && DeletedAt is null)
        {
            return;
        }

        Archived = false;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>ISoftDeletable.Delete → archive.</summary>
    public void SoftDelete() => Archive();

    public int DaysUntilRenewal(DateOnly? asOf = null)
    {
        var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return NextRenewalDate.DayNumber - today.DayNumber;
    }

    public bool IsUpcoming(int withinDays = 3, DateOnly? asOf = null)
    {
        var days = DaysUntilRenewal(asOf);
        return IsActive && days >= 0 && days <= withinDays;
    }

    public bool IsOverdue(DateOnly? asOf = null)
    {
        return IsActive && DaysUntilRenewal(asOf) < 0;
    }

    private void ApplyValues(
        Guid userId,
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        Guid? providerId,
        Guid? categoryId,
        Guid? userCategoryId,
        string? notes,
        bool archived)
    {
        UserId = userId;
        ProviderId = providerId;
        CategoryId = categoryId;
        UserCategoryId = userCategoryId;
        Name = name.Trim();
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
        BillingCycle = billingCycle;
        SharedWithCount = sharedWithCount;
        NextRenewalDate = nextRenewalDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Archived = archived;
    }

    private static Result Validate(
        Guid userId,
        string name,
        decimal price,
        string currency,
        BillingCycle billingCycle,
        int sharedWithCount,
        DateOnly nextRenewalDate,
        Guid? categoryId,
        Guid? userCategoryId,
        string? notes,
        DateOnly? today,
        bool requireFutureRenewal = true)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(DomainErrors.UserErrors.UnAuthorized);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > SubscriptionConstants.NameMaxLength)
        {
            return Result.Failure(DomainErrors.ValidationErrors.ValidationFailed);
        }

        if (price <= 0)
        {
            return Result.Failure(DomainErrors.Subscription.InvalidPrice);
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length > SubscriptionConstants.CurrencyMaxLength)
        {
            return Result.Failure(DomainErrors.ProfileErrors.InvalidCurrency);
        }

        if (!Enum.IsDefined(billingCycle))
        {
            return Result.Failure(DomainErrors.Subscription.InvalidBillingCycle);
        }

        if (sharedWithCount < SubscriptionConstants.MinSharedWithCount)
        {
            return Result.Failure(DomainErrors.Subscription.InvalidSharedCount);
        }

        if (categoryId.HasValue && userCategoryId.HasValue)
        {
            return Result.Failure(DomainErrors.Subscription.CategoryConflict);
        }

        if (requireFutureRenewal)
        {
            var asOf = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (nextRenewalDate < asOf)
            {
                return Result.Failure(DomainErrors.Subscription.InvalidRenewalDate);
            }
        }

        if (notes is { Length: > SubscriptionConstants.NotesMaxLength })
        {
            return Result.Failure(DomainErrors.ValidationErrors.MaxLengthExceeded);
        }

        return Result.Success();
    }
}

