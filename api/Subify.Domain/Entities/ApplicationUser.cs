using Microsoft.AspNetCore.Identity;

namespace Subify.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Locate { get; set; } = "tr";
    public string MainCurrency { get; set; } = "TRY";
    public decimal? MonthlyBudget { get; set; }
    public string ApplicationThemeColor { get; set; } = "Royal Purple";
    public bool DarkTheme { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}