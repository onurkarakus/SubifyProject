using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>
/// Per-user notification preferences (task 3.2.11).
/// </summary>
public class NotificationSetting : BaseEntity
{
    public const int DefaultDaysBeforeRenewal = 3;

    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public int DaysBeforeRenewal { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    protected NotificationSetting()
    {
    }

    /// <summary>
    /// Defaults for a newly registered user: email off (no mail motor yet), push off, 3 days in-app.
    /// </summary>
    public static NotificationSetting CreateDefaults(Guid userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        return new NotificationSetting
        {
            Id = GuidGenerator.NewId(),
            UserId = userId,
            EmailEnabled = false,
            PushEnabled = false,
            DaysBeforeRenewal = DefaultDaysBeforeRenewal,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateSettings(bool emailEnabled, bool pushEnabled, int daysBeforeRenewal)
    {
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        DaysBeforeRenewal = daysBeforeRenewal < 0 ? 0 : daysBeforeRenewal;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
