using Subify.Domain.Enums;
using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.AuditLogs;

public class ActivityLog: BaseEntity
{
    public Guid? UserId { get; set; }

    public ActivityLogEntityType EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public ActivityLogAction Action { get; set; }

    public string Description { get; set; } = null!;

    public string OldValues { get; set; } = null!;

    public string NewValues { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;    

    public string? Details { get; set; }

    public ApplicationUser? User { get; set; }    
}
