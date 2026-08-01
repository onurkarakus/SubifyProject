using Subify.Domain.Services;

namespace Subify.Application.Features.Subscriptions;

/// <summary>
/// List/dashboard financial summary (4.1.5 / 4.3.4 / 4.3.5).
/// Currency = user MainCurrency. Unconvertible foreign amounts → Warnings.
/// Budget: exceeded when monthlyBudget &gt; 0 and monthlyTotal &gt; budget.
/// </summary>
public sealed record SubscriptionListSummary(
    decimal MonthlyTotal,
    decimal YearlyTotal,
    string Currency,
    IReadOnlyList<string> Warnings,
    bool HasUnconvertedAmounts,
    decimal? MonthlyBudget,
    bool IsBudgetExceeded)
{
    public static SubscriptionListSummary FromTotals(
        SubscriptionTotalsSummary totals,
        decimal? monthlyBudget) =>
        new(
            MonthlyTotal: totals.MonthlyTotal,
            YearlyTotal: totals.YearlyTotal,
            Currency: totals.Currency,
            Warnings: totals.Warnings,
            HasUnconvertedAmounts: totals.HasUnconvertedAmounts,
            MonthlyBudget: monthlyBudget is > 0 ? monthlyBudget : null,
            IsBudgetExceeded: BudgetRules.IsExceeded(totals.MonthlyTotal, monthlyBudget));
}
