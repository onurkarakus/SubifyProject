using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    protected ActivityLog()
    {
    }

    /// <summary>Legacy ctor kept for callers that do not set entity id / snapshots.</summary>
    public ActivityLog(
        Guid userId,
        string entityType,
        string action,
        string description,
        string? ipAddress,
        string? userAgent)
        : this(userId, entityType, action, description, entityId: null, oldValues: null, newValues: null, ipAddress, userAgent)
    {
    }

    private ActivityLog(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent)
    {
        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        Description = description;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates an audit row with optional entity link and JSON snapshots (4.1.2+).
    /// </summary>
    public static ActivityLog Create(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new ActivityLog(
            userId,
            entityType.Trim(),
            action.Trim(),
            description.Trim(),
            entityId,
            oldValues,
            newValues,
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim());
    }
}
