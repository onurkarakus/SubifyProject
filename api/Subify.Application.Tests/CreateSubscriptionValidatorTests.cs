using FluentValidation.TestHelper;
using Subify.Application.Features.Subscriptions.CreateSubscription;

namespace Subify.Application.Tests;

/// <summary>12.1.4 / 12.1.5 — FluentValidation for create subscription (incl. category XOR).</summary>
public class CreateSubscriptionValidatorTests
{
    private readonly CreateSubscriptionValidator _validator = new();
    private static readonly DateOnly Future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

    private static CreateSubscriptionCommand Build(
        string name = "Netflix",
        decimal price = 100m,
        string currency = "TRY",
        string cycle = "monthly",
        int shared = 1,
        DateOnly? renewal = null,
        Guid? categoryId = null,
        Guid? userCategoryId = null,
        string? notes = null) =>
        new(name, price, currency, cycle, shared, renewal ?? Future,
            CategoryId: categoryId, UserCategoryId: userCategoryId, Notes: notes);

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Build());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Price_must_be_positive(decimal price)
    {
        var result = _validator.TestValidate(Build(price: price));
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData("")]
    [InlineData("weekly")]
    [InlineData("daily")]
    public void BillingCycle_must_be_monthly_or_yearly(string cycle)
    {
        var result = _validator.TestValidate(Build(cycle: cycle));
        result.ShouldHaveValidationErrorFor(x => x.BillingCycle);
    }

    [Theory]
    [InlineData("XXX")]
    [InlineData("btc")]
    public void Currency_must_be_supported(string currency)
    {
        var result = _validator.TestValidate(Build(currency: currency));
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Category_XOR_rejects_both_ids()
    {
        var result = _validator.TestValidate(Build(
            categoryId: Guid.NewGuid(),
            userCategoryId: Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Category_or_userCategory_alone_ok()
    {
        Assert.True(_validator.TestValidate(Build(categoryId: Guid.NewGuid())).IsValid);
        Assert.True(_validator.TestValidate(Build(userCategoryId: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void SharedWithCount_min_1()
    {
        var result = _validator.TestValidate(Build(shared: 0));
        result.ShouldHaveValidationErrorFor(x => x.SharedWithCount);
    }

    [Fact]
    public void Name_required()
    {
        var result = _validator.TestValidate(Build(name: ""));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
