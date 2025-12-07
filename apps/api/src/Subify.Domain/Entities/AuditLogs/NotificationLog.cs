using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Subscriptions;
using Subify.Domain.Entities.Users;
using Subify.Domain.Enums;

namespace Subify.Domain.Entities.AuditLogs;

/// <summary>
/// Audit log for all sent notifications.
/// </summary>
public sealed class NotificationLog : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Notification type. 
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Delivery channel.
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Notification body content.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Delivery status.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Sent;

    /// <summary>
    /// Error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the notification was sent.
    /// </summary>
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Subscription? Subscription { get; set; }
}
