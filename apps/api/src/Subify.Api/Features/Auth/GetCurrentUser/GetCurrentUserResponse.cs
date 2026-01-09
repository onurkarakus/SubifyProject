namespace Subify.Api.Features.Auth.GetCurrentUser;

public record GetCurrentUserResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastLoginAt,
    string Plan,
    bool DarkTheme,
    string ApplicationThemeColor,
    decimal MonthlyBudget,
    string UserName,
    string Email, 
    string FullName, 
    string MainCurrency);
