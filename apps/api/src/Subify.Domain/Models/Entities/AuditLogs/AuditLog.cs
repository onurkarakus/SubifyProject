using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.AuditLogs;

public class AuditLog: BaseEntity
{
    public Guid? UserId { get; set; }

    public string EventType { get; set; } = null!;

    public string? EventData { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsSuccessful { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; }
}
