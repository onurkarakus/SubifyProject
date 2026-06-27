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

    protected ActivityLog() { }

    public ActivityLog(Guid userId, string entityType, string action, string description, string? ipAddress, string? userAgent)
    {
        UserId = userId;
        EntityType = entityType;
        Action = action;
        Description = description;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}