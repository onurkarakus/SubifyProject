namespace Subify.Domain.Services;

/// <summary>Monthly budget checks (4.3.5).</summary>
public static class BudgetRules
{
    /// <summary>
    /// True when a positive budget is set and <paramref name="monthlyTotal"/> exceeds it.
    /// Null / ≤0 budget → tracking disabled → never exceeded.
    /// </summary>
    public static bool IsExceeded(decimal monthlyTotal, decimal? monthlyBudget) =>
        monthlyBudget is > 0 && monthlyTotal > monthlyBudget.Value;
}
