using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Subify.Domain.Models.Entities.Users;

public sealed class Profile : BaseEntity
{
    // Id inherited from BaseEntity, serves as both PK and FK to ApplicationUser

    public Guid? UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    /// <summary>
    /// ISO 639-1 language code.  'tr' or 'en'.
    /// </summary>
    public string Locale { get; set; } = "tr-TR";

    public string ApplicationThemeColor { get; set; } = "Royal Purple";

    public bool DarkTheme { get; set; } = false;

    public string MainCurrency { get; set; } = "TRY";

    public decimal MonthlyBudget { get; set; } = 0m;

    /// <summary>
    /// Subscription plan.  'free' or 'premium'.
    /// </summary>
    public PlanType Plan { get; set; } = PlanType.Free;

    /// <summary>
    /// When the premium plan renews/expires.
    /// </summary>
    public DateTimeOffset? PlanRenewsAt { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public NotificationSetting? NotificationSettings { get; set; }
}
