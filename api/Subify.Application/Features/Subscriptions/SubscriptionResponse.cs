using Subify.Domain.Entities;
using Subify.Domain.Enums;

namespace Subify.Application.Features.Subscriptions;

/// <summary>
/// Subscription detail/list item DTO (4.1.3 / 4.1.10).
/// Includes userShare and nested category/provider when navigations are loaded.
/// </summary>
public sealed record SubscriptionResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    BillingCycle BillingCycle,
    int SharedWithCount,
    decimal UserShare,
    decimal MonthlyEquivalentShare,
    decimal YearlyEquivalentShare,
    DateOnly NextRenewalDate,
    Guid? ProviderId,
    Guid? CategoryId,
    Guid? UserCategoryId,
    DateOnly? LastUsedAt,
    string? Notes,
    bool Archived,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    SubscriptionCategoryRef? Category,
    SubscriptionProviderRef? Provider)
{
    /// <summary>
    /// Maps entity. Nested refs use navigation properties when loaded via Include;
    /// otherwise Category/Provider are null (ids still present).
    /// </summary>
    public static SubscriptionResponse FromEntity(Subscription entity)
    {
        SubscriptionCategoryRef? category = null;
        if (entity.UserCategory is not null)
        {
            category = SubscriptionCategoryRef.FromUser(entity.UserCategory);
        }
        else if (entity.Category is not null)
        {
            category = SubscriptionCategoryRef.FromSystem(entity.Category);
        }

        var provider = entity.Provider is not null
            ? SubscriptionProviderRef.FromEntity(entity.Provider)
            : null;

        return new(
            Id: entity.Id,
            Name: entity.Name,
            Price: entity.Price,
            Currency: entity.Currency,
            BillingCycle: entity.BillingCycle,
            SharedWithCount: entity.SharedWithCount,
            UserShare: entity.UserShare,
            MonthlyEquivalentShare: entity.MonthlyEquivalentShare,
            YearlyEquivalentShare: entity.YearlyEquivalentShare,
            NextRenewalDate: entity.NextRenewalDate,
            ProviderId: entity.ProviderId,
            CategoryId: entity.CategoryId,
            UserCategoryId: entity.UserCategoryId,
            LastUsedAt: entity.LastUsedAt,
            Notes: entity.Notes,
            Archived: entity.Archived,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            Category: category,
            Provider: provider);
    }
}
