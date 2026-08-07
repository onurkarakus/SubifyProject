namespace Subify.Application.Features.Reports;

/// <summary>One calendar month point for the monthly-spend chart (6.1.1).</summary>
public sealed record MonthlySpendPoint(string Month, decimal Total);

/// <summary>
/// Monthly spend series. Empty <see cref="Data"/> + <see cref="Message"/> when no subscriptions (6.1.4).
/// </summary>
public sealed record MonthlySpendResponse(
    IReadOnlyList<MonthlySpendPoint> Data,
    string Currency,
    decimal Average,
    string? Message = null);

/// <summary>Category slice for pie/bar chart (6.1.2).</summary>
public sealed record CategoryBreakdownItem(
    string Category,
    string Name,
    string? Color,
    decimal Total,
    decimal Percentage,
    int Count);

/// <summary>
/// Category breakdown. Empty <see cref="Data"/> + <see cref="Message"/> when none (6.1.4).
/// </summary>
public sealed record CategoryBreakdownResponse(
    IReadOnlyList<CategoryBreakdownItem> Data,
    decimal GrandTotal,
    string Currency,
    string? Message = null);

/// <summary>Per original-currency slice (6.1.3).</summary>
public sealed record CurrencyDistributionItem(
    string Currency,
    decimal MonthlyTotal,
    decimal ConvertedMonthlyTotal,
    decimal Percentage,
    int Count);

/// <summary>
/// Currency distribution. Empty <see cref="Data"/> + <see cref="Message"/> when none (6.1.4).
/// </summary>
public sealed record CurrencyDistributionResponse(
    IReadOnlyList<CurrencyDistributionItem> Data,
    decimal GrandTotal,
    string Currency,
    string? Message = null);
