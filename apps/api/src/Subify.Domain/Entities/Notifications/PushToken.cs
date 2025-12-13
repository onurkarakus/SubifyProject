using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Entities.Notifications;

/// <summary>
/// FCM/APNS push notification tokens.
/// A user can have multiple tokens (multiple devices).
/// </summary>
public sealed class PushToken : BaseEntity
{
    public Guid? UserId { get; set; }

    /// <summary>
    /// FCM/APNS token string.
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Device platform: 'ios', 'android', 'web'
    /// </summary>
    public string Platform { get; set; } = null!;

    /// <summary>
    /// Unique device identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Device name for user reference.
    /// Example: 'iPhone 15 Pro'
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Token is active and deliverable.
    /// Set to false when token becomes invalid.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last successful push delivery or registration.
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; } = null!;
}
