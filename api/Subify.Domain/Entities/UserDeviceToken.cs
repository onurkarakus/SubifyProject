using Subify.Domain.Common;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities;

/// <summary>
/// Push device registration (FCM/APNs). Used by Flutter phase; entity ready early.
/// No freemium restriction — any authenticated user may register a token later.
/// </summary>
public class UserDeviceToken : BaseEntity
{
    public const int TokenMaxLength = 512;
    public const int DeviceNameMaxLength = 200;

    public Guid UserId { get; private set; }

    /// <summary>FCM / APNs / web push token string.</summary>
    public string Token { get; private set; } = null!;

    public DevicePlatform Platform { get; private set; }

    /// <summary>Optional human-readable device label (e.g. "Onur iPhone").</summary>
    public string? DeviceName { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Last time this token was confirmed/used by the client.</summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    protected UserDeviceToken()
    {
    }

    public static UserDeviceToken Create(
        Guid userId,
        string token,
        DevicePlatform platform,
        string? deviceName = null,
        DateTimeOffset? utcNow = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!Enum.IsDefined(platform) || platform == DevicePlatform.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(platform), "Platform must be Android, Ios, or Web.");
        }

        var now = utcNow ?? DateTimeOffset.UtcNow;
        var normalizedToken = token.Trim();

        if (normalizedToken.Length > TokenMaxLength)
        {
            throw new ArgumentException($"Token exceeds max length of {TokenMaxLength}.", nameof(token));
        }

        return new UserDeviceToken
        {
            Id = GuidGenerator.NewId(),
            UserId = userId,
            Token = normalizedToken,
            Platform = platform,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            IsActive = true,
            LastSeenAt = now,
            CreatedAt = now
        };
    }

    /// <summary>Refresh last-seen and optionally rename when the client re-registers the same token.</summary>
    public void Touch(string? deviceName = null, DateTimeOffset? utcNow = null)
    {
        LastSeenAt = utcNow ?? DateTimeOffset.UtcNow;
        IsActive = true;

        if (deviceName is not null)
        {
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
        }

        UpdatedAt = LastSeenAt;
    }

    public void Deactivate(DateTimeOffset? utcNow = null)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = utcNow ?? DateTimeOffset.UtcNow;
    }

    public void Activate(DateTimeOffset? utcNow = null)
    {
        IsActive = true;
        LastSeenAt = utcNow ?? DateTimeOffset.UtcNow;
        UpdatedAt = LastSeenAt;
    }
}
