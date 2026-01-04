using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.Register;

public record RegisterCommand(
    string Email, 
    string Password, 
    string FullName,
    string MainCurrency,
    string Locale,
    bool UseDarkTheme,
    decimal MonthlyBudget,
    string ApplicationThemeColor,
    int DaysBeforeRenewal,
    bool NotificationEmailEnabled,
    bool NotificationPushEnabled) : IRequest<Result<RegisterResponse>>;
