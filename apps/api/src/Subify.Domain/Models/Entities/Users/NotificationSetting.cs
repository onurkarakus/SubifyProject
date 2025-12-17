using Subify.Domain.Models.Entities.Common;

namespace Subify.Domain.Models.Entities.Users;

/// <summary>
/// User notification preferences.
/// Uses shared primary key pattern (Id = Profile.Id = User.Id).
/// </summary>
public sealed class NotificationSetting : BaseEntity
{
    // Id = User.Id (shared PK pattern)

    /// <summary>
    /// Email notifications enabled.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Push notifications enabled (Premium only).
    /// </summary>
    public bool PushEnabled { get; set; } = false;

    /// <summary>
    /// Days before renewal to send reminder.
    /// </summary>
    public int DaysBeforeRenewal { get; set; } = 3;

    // Navigation
    public Profile Profile { get; set; } = null!;
}
