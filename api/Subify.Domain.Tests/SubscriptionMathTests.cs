using Subify.Domain.Enums;
using Subify.Domain.Services;

namespace Subify.Domain.Tests;

/// <summary>4.3.1 / 4.3.2 / 4.3.3 / 4.3.4 unit tests (see also FinancialMotorTests for 4.3.6 pipeline).</summary>
public class SubscriptionMathTests
{
    [Fact]
    public void UserShare_with_zero_shared_returns_full_price()
    {
        // Domain rejects shared &lt; 1 on create; math still defends against divide-by-zero.
        Assert.Equal(100m, SubscriptionMath.UserShare(100m, 0));
    }

    [Theory]
    [InlineData(100, 4, 25)]
    [InlineData(149.99, 2, 75.00)]
    [InlineData(10, 3, 3.33)]
    public void UserShare_rounds_away_from_zero(decimal price, int shared, decimal expected)
    {
        Assert.Equal(expected, SubscriptionMath.UserShare(price, shared));
    }

    [Fact]
    public void Monthly_and_yearly_equivalents()
    {
        Assert.Equal(100m, SubscriptionMath.MonthlyEquivalent(100m, BillingCycle.Monthly));
        Assert.Equal(1200m, SubscriptionMath.YearlyEquivalent(100m, BillingCycle.Monthly));

        Assert.Equal(100m, SubscriptionMath.MonthlyEquivalent(1200m, BillingCycle.Yearly));
        Assert.Equal(1200m, SubscriptionMath.YearlyEquivalent(1200m, BillingCycle.Yearly));
    }

    [Fact]
    public void SumInCurrency_only_main_currency_without_rates()
    {
        var lines = new SubscriptionAmountLine[]
        {
            new(100m, 1, BillingCycle.Monthly, "TRY"),
            new(1200m, 1, BillingCycle.Yearly, "TRY"), // +100 monthly
            new(50m, 1, BillingCycle.Monthly, "USD"),  // no rate → warning, excluded
            new(40m, 2, BillingCycle.Monthly, "try"),  // +20 monthly
        };

        var summary = SubscriptionMath.SumInCurrency(lines, "try");

        Assert.Equal("TRY", summary.Currency);
        Assert.Equal(220m, summary.MonthlyTotal); // 100 + 100 + 20
        Assert.Equal(2640m, summary.YearlyTotal); // 1200 + 1200 + 240
        Assert.True(summary.HasUnconvertedAmounts);
        Assert.Contains(summary.Warnings, w => w.Contains("USD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SumConverted_applies_fx_rates()
    {
        var lines = new SubscriptionAmountLine[]
        {
            new(100m, 1, BillingCycle.Monthly, "TRY"),
            new(10m, 1, BillingCycle.Monthly, "USD"), // 10 * 30 = 300
        };

        var rates = new Dictionary<(string, string), decimal>
        {
            [("USD", "TRY")] = 30m
        };

        var summary = SubscriptionMath.SumConverted(lines, "TRY", rates);
        Assert.Equal(400m, summary.MonthlyTotal);
        Assert.False(summary.HasUnconvertedAmounts);
        Assert.Empty(summary.Warnings);
    }
}
