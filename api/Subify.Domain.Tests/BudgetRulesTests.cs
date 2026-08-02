using Subify.Domain.Services;

namespace Subify.Domain.Tests;

/// <summary>Task 4.3.5 — budget exceeded flag.</summary>
public class BudgetRulesTests
{
    [Fact]
    public void Null_or_non_positive_budget_never_exceeded()
    {
        Assert.False(BudgetRules.IsExceeded(100m, null));
        Assert.False(BudgetRules.IsExceeded(100m, 0m));
        Assert.False(BudgetRules.IsExceeded(100m, -10m));
    }

    [Fact]
    public void Equal_budget_is_not_exceeded()
    {
        Assert.False(BudgetRules.IsExceeded(100m, 100m));
    }

    [Fact]
    public void Over_budget_is_exceeded()
    {
        Assert.True(BudgetRules.IsExceeded(100.01m, 100m));
        Assert.True(BudgetRules.IsExceeded(200m, 150m));
    }

    [Fact]
    public void Under_budget_is_not_exceeded()
    {
        Assert.False(BudgetRules.IsExceeded(50m, 200m));
    }
}
