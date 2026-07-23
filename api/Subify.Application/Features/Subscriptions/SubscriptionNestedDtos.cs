using Subify.Domain.Entities;

namespace Subify.Application.Features.Subscriptions;

/// <summary>Nested category on subscription responses (4.1.10). System or user-owned.</summary>
public sealed record SubscriptionCategoryRef(
    Guid Id,
    string? Slug,
    string Name,
    string? Icon,
    string? Color,
    bool IsUserCategory)
{
    public static SubscriptionCategoryRef FromSystem(Category category) =>
        new(
            Id: category.Id,
            Slug: category.Slug,
            // Display names live in Resources; slug is a stable fallback until i18n join.
            Name: category.Slug,
            Icon: category.Icon,
            Color: category.Color,
            IsUserCategory: false);

    public static SubscriptionCategoryRef FromUser(UserCategory category) =>
        new(
            Id: category.Id,
            Slug: null,
            Name: category.Name,
            Icon: category.Icon,
            Color: category.Color,
            IsUserCategory: true);
}

/// <summary>Nested provider on subscription responses (4.1.10).</summary>
public sealed record SubscriptionProviderRef(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    bool IsActive)
{
    public static SubscriptionProviderRef FromEntity(Provider provider) =>
        new(
            Id: provider.Id,
            Name: provider.Name,
            Slug: provider.Slug,
            LogoUrl: provider.LogoUrl,
            IsActive: provider.IsActive);
}
