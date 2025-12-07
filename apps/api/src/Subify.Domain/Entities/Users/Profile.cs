using Subify.Domain.Entities.Common;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities.Users;

public class Profile : BaseEntity
{
    // Id inherited from BaseEntity, serves as both PK and FK to ApplicationUser

    public string Email { get; set; } = null!;
    public string? FullName { get; set; }

    /// <summary>
    /// ISO 639-1 language code.  'tr' or 'en'.
    /// </summary>
    public string Locale { get; set; } = "tr";

    /// <summary>
    /// Subscription plan.  'free' or 'premium'.
    /// </summary>
    public PlanType Plan { get; set; } = PlanType.Free;

    /// <summary>
    /// When the premium plan renews/expires.
    /// </summary>
    public DateTimeOffset? PlanRenewsAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public NotificationSettings? NotificationSettings { get; set; }
}
