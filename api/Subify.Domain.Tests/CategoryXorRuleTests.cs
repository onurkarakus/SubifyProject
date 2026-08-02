using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;

namespace Subify.Domain.Tests;

/// <summary>12.1.5 — Category XOR domain rule (system category vs user category).</summary>
public class CategoryXorRuleTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateOnly Today = new(2026, 8, 1);

    [Fact]
    public void Create_allows_system_category_only()
    {
        var result = Subscription.Create(
            UserId, "Netflix", 100m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5),
            categoryId: Guid.NewGuid(), today: Today);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.CategoryId);
        Assert.Null(result.Value.UserCategoryId);
    }

    [Fact]
    public void Create_allows_user_category_only()
    {
        var result = Subscription.Create(
            UserId, "Custom", 20m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5),
            userCategoryId: Guid.NewGuid(), today: Today);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CategoryId);
        Assert.NotNull(result.Value.UserCategoryId);
    }

    [Fact]
    public void Create_allows_neither_category()
    {
        var result = Subscription.Create(
            UserId, "Misc", 5m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5), today: Today);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_rejects_both_categories()
    {
        var result = Subscription.Create(
            UserId, "Bad", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5),
            categoryId: Guid.NewGuid(),
            userCategoryId: Guid.NewGuid(),
            today: Today);
        Assert.Equal(DomainErrors.Subscription.CategoryConflict.Code, result.Error.Code);
    }

    [Fact]
    public void Update_rejects_both_categories()
    {
        var sub = Subscription.Create(
            UserId, "X", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5), today: Today).Value;

        var update = sub.Update(
            "X", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(5),
            categoryId: Guid.NewGuid(),
            userCategoryId: Guid.NewGuid(),
            today: Today);

        Assert.Equal(DomainErrors.Subscription.CategoryConflict.Code, update.Error.Code);
    }
}
