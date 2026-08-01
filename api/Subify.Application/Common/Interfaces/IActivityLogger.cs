namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Central activity/audit writer (5.4.1).
/// Adds a row to the unit of work; callers still call <c>SaveChangesAsync</c> unless using
/// <see cref="LogAndSaveAsync"/>.
/// </summary>
public interface IActivityLogger
{
    /// <summary>Stage an activity row (IP/User-Agent from HTTP when available).</summary>
    Task LogAsync(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stage and immediately commit the activity row (for post-Identity SaveChanges flows).</summary>
    Task LogAndSaveAsync(
        Guid userId,
        string entityType,
        string action,
        string description,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default);
}
