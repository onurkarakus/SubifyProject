using Subify.Domain.Enums;
using Subify.Domain.Services;

namespace Subify.Domain.Tests;

/// <summary>
/// Task 4.3.6 — consolidated financial motor coverage:
/// userShare, monthly/yearly equivalents, FX convert, totals, budget flag.
/// </summary>
public class FinancialMotorTests
{
    [Fact]
    public void Pipeline_share_equivalents_fx_sum_and_budget()
    {
        // --- 4.3.1 share ---
        // 149.99 / 4 = 37.4975 → 37.50
        Assert.Equal(37.50m, SubscriptionMath.UserShare(149.99m, 4));

        // --- 4.3.2 monthly / yearly ---
        var monthlyShare = SubscriptionMath.UserShare(120m, 2); // 60
        Assert.Equal(60m, SubscriptionMath.MonthlyEquivalent(monthlyShare, BillingCycle.Monthly));
        Assert.Equal(720m, SubscriptionMath.YearlyEquivalent(monthlyShare, BillingCycle.Monthly));

        var yearlyShare = SubscriptionMath.UserShare(1200m, 1); // 1200
        Assert.Equal(100m, SubscriptionMath.MonthlyEquivalent(yearlyShare, BillingCycle.Yearly));
        Assert.Equal(1200m, SubscriptionMath.YearlyEquivalent(yearlyShare, BillingCycle.Yearly));

        // --- 4.3.3 + 4.3.4 totals with FX ---
        var lines = new SubscriptionAmountLine[]
        {
            // 100 TRY monthly
            new(100m, 1, BillingCycle.Monthly, "TRY"),
            // 1200 TRY yearly → 100 / mo
            new(1200m, 1, BillingCycle.Yearly, "TRY"),
            // 20 USD monthly * 32 = 640 TRY
            new(20m, 1, BillingCycle.Monthly, "USD"),
            // 40 EUR monthly — no rate → warning + excluded
            new(40m, 1, BillingCycle.Monthly, "EUR"),
        };

        var rates = new Dictionary<(string, string), decimal>
        {
            [("USD", "TRY")] = 32m
        };

        var totals = SubscriptionMath.SumConverted(lines, "TRY", rates);
        // 100 + 100 + 640 = 840 monthly
        Assert.Equal(840m, totals.MonthlyTotal);
        // yearly: 1200 + 1200 + (640*12) = 2400 + 7680 = 10080
        Assert.Equal(10080m, totals.YearlyTotal);
        Assert.Equal("TRY", totals.Currency);
        Assert.True(totals.HasUnconvertedAmounts);
        Assert.Contains(totals.Warnings, w => w.Contains("EUR", StringComparison.OrdinalIgnoreCase));

        // --- 4.3.5 budget ---
        Assert.True(BudgetRules.IsExceeded(totals.MonthlyTotal, 800m));
        Assert.False(BudgetRules.IsExceeded(totals.MonthlyTotal, 900m));
        Assert.False(BudgetRules.IsExceeded(totals.MonthlyTotal, null));
    }

    [Fact]
    public void FromPrice_helpers_match_chained_share_and_cycle()
    {
        Assert.Equal(
            SubscriptionMath.MonthlyEquivalent(SubscriptionMath.UserShare(99m, 3), BillingCycle.Monthly),
            SubscriptionMath.MonthlyEquivalentFromPrice(99m, 3, BillingCycle.Monthly));

        Assert.Equal(
            SubscriptionMath.YearlyEquivalent(SubscriptionMath.UserShare(2400m, 2), BillingCycle.Yearly),
            SubscriptionMath.YearlyEquivalentFromPrice(2400m, 2, BillingCycle.Yearly));
    }

    [Fact]
    public void Empty_lines_yield_zero_totals_no_warnings()
    {
        var totals = SubscriptionMath.SumConverted(
            Array.Empty<SubscriptionAmountLine>(),
            "USD",
            new Dictionary<(string, string), decimal>());

        Assert.Equal(0m, totals.MonthlyTotal);
        Assert.Equal(0m, totals.YearlyTotal);
        Assert.Equal("USD", totals.Currency);
        Assert.False(totals.HasUnconvertedAmounts);
        Assert.Empty(totals.Warnings);
    }

    [Fact]
    public void Shared_count_one_keeps_full_price_as_user_share()
    {
        Assert.Equal(49.99m, SubscriptionMath.UserShare(49.99m, 1));
    }
}
