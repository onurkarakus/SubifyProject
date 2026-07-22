namespace Subify.Domain.Common;

/// <summary>
/// Base type for domain entities with GUID primary key and audit timestamps.
/// </summary>
/// <remarks>
/// <see cref="Id"/> is not initialized here (no <c>Guid.NewGuid()</c> on property).
/// IDs are assigned by factories via <see cref="GuidGenerator.NewId"/> or by
/// <c>SubifyDbContext</c> on insert when still empty. See task 2.1.10 / ADR-010 (OS).
/// </remarks>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
