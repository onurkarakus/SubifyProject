namespace Subify.Application.Features.Profile;

/// <summary>Current user profile preferences (5.3.1). No plan/premium fields (Subify OS).</summary>
public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string Locale,
    string MainCurrency,
    decimal? MonthlyBudget,
    string ApplicationThemeColor,
    bool DarkTheme,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
