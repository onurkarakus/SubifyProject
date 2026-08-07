using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>
/// High-level send with template + optional dedupe (15.3.2).
/// Logs delivery attempts for audit / anti-duplicate.
/// </summary>
public interface IEmailDeliveryService
{
    /// <param name="dedupeKey">
    /// When set, a prior successful send with the same key skips re-send (success Result, no SMTP call).
    /// Example: <c>renewal:{subscriptionId}:{yyyy-MM-dd}</c>.
    /// </param>
    Task<Result> SendTemplatedAsync(
        string templateName,
        string? locale,
        string toEmail,
        IReadOnlyDictionary<string, string> tokens,
        Guid? userId = null,
        Guid? relatedEntityId = null,
        string? dedupeKey = null,
        CancellationToken cancellationToken = default);
}
