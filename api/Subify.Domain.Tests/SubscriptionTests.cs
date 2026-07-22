using Subify.Domain.Entities;
using Subify.Domain.Enums;
using Subify.Domain.Errors;

namespace Subify.Domain.Tests;

public class SubscriptionTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Today = new(2026, 3, 22);

    [Theory]
    [InlineData(100, 4, 25)]
    [InlineData(149.99, 1, 149.99)]
    [InlineData(10, 3, 3.33)]
    public void UserShare_divides_price_by_shared_count(decimal price, int shared, decimal expected)
    {
        var result = Subscription.Create(
            UserId,
            "Netflix",
            price,
            "TRY",
            BillingCycle.Monthly,
            shared,
            Today.AddDays(10),
            today: Today);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.UserShare);
    }

    [Fact]
    public void Create_rejects_category_xor_conflict()
    {
        var result = Subscription.Create(
            UserId,
            "Gym",
            50,
            "TRY",
            BillingCycle.Monthly,
            1,
            Today.AddDays(5),
            categoryId: Guid.NewGuid(),
            userCategoryId: Guid.NewGuid(),
            today: Today);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Subscription.CategoryConflict.Code, result.Error.Code);
    }

    [Fact]
    public void Create_rejects_invalid_price_and_shared_count()
    {
        var price = Subscription.Create(UserId, "X", 0, "TRY", BillingCycle.Monthly, 1, Today.AddDays(1), today: Today);
        Assert.Equal(DomainErrors.Subscription.InvalidPrice.Code, price.Error.Code);

        var share = Subscription.Create(UserId, "X", 10, "TRY", BillingCycle.Monthly, 0, Today.AddDays(1), today: Today);
        Assert.Equal(DomainErrors.Subscription.InvalidSharedCount.Code, share.Error.Code);
    }

    [Fact]
    public void Archive_and_Reactivate_toggle_active_state()
    {
        var sub = Subscription.Create(
            UserId, "Spotify", 59.99m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(7), today: Today).Value;

        Assert.True(sub.IsActive);
        sub.Archive();
        Assert.False(sub.IsActive);
        Assert.True(sub.Archived);
        Assert.NotNull(sub.DeletedAt);

        sub.Reactivate();
        Assert.True(sub.IsActive);
        Assert.False(sub.Archived);
        Assert.Null(sub.DeletedAt);
    }

    [Fact]
    public void Monthly_and_yearly_equivalents()
    {
        var monthly = Subscription.Create(
            UserId, "M", 100m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(1), today: Today).Value;
        Assert.Equal(100m, monthly.MonthlyEquivalentShare);
        Assert.Equal(1200m, monthly.YearlyEquivalentShare);

        var yearly = Subscription.Create(
            UserId, "Y", 1200m, "TRY", BillingCycle.Yearly, 1, Today.AddDays(1), today: Today).Value;
        Assert.Equal(100m, yearly.MonthlyEquivalentShare);
        Assert.Equal(1200m, yearly.YearlyEquivalentShare);
    }

    [Fact]
    public void IsUpcoming_and_IsOverdue()
    {
        var upcoming = Subscription.Create(
            UserId, "Soon", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(2), today: Today).Value;
        Assert.True(upcoming.IsUpcoming(3, Today));
        Assert.False(upcoming.IsOverdue(Today));

        var overdue = Subscription.Create(
            UserId, "Late", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(1), today: Today).Value;
        // Update to past renewal without requireFutureRenewal
        overdue.Update("Late", 10m, "TRY", BillingCycle.Monthly, 1, Today.AddDays(-1), today: Today);
        Assert.True(overdue.IsOverdue(Today));
    }
}
