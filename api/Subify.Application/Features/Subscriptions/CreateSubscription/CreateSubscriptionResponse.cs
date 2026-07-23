using Subify.Domain.Enums;

namespace Subify.Application.Features.Subscriptions.CreateSubscription;

/// <summary>Create result — aligned with <see cref="SubscriptionResponse"/> (4.1.10).</summary>
public sealed record CreateSubscriptionResponse(
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
    public static CreateSubscriptionResponse FromSubscription(SubscriptionResponse dto) =>
        new(
            Id: dto.Id,
            Name: dto.Name,
            Price: dto.Price,
            Currency: dto.Currency,
            BillingCycle: dto.BillingCycle,
            SharedWithCount: dto.SharedWithCount,
            UserShare: dto.UserShare,
            MonthlyEquivalentShare: dto.MonthlyEquivalentShare,
            YearlyEquivalentShare: dto.YearlyEquivalentShare,
            NextRenewalDate: dto.NextRenewalDate,
            ProviderId: dto.ProviderId,
            CategoryId: dto.CategoryId,
            UserCategoryId: dto.UserCategoryId,
            LastUsedAt: dto.LastUsedAt,
            Notes: dto.Notes,
            Archived: dto.Archived,
            CreatedAt: dto.CreatedAt,
            UpdatedAt: dto.UpdatedAt,
            Category: dto.Category,
            Provider: dto.Provider);
}
