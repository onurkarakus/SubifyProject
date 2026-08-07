using System.Text.Json;
using Subify.Domain.Entities;

namespace Subify.Application.Features.Profile;

/// <summary>JSON snapshots for profile activity logs (5.3.6). Email excluded from change noise optional — kept for audit.</summary>
internal static class ProfileActivitySnapshots
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Capture(ApplicationUser user) =>
        JsonSerializer.Serialize(
            new
            {
                user.Id,
                user.FullName,
                user.Locale,
                user.MainCurrency,
                user.MonthlyBudget,
                user.ApplicationThemeColor,
                user.DarkTheme
            },
            Json);
}
