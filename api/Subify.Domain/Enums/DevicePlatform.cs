namespace Subify.Domain.Enums;

/// <summary>
/// Push notification client platform (FCM / APNs / web push).
/// </summary>
public enum DevicePlatform
{
    Unknown = 0,
    Android = 1,
    Ios = 2,
    Web = 3
}
