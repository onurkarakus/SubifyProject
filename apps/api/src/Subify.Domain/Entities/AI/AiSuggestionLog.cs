using Subify.Domain.Entities.Common;
using Subify.Domain.Entities.Users;

namespace Subify.Domain.Entities.AI;

/// <summary>
/// Logs AI suggestion requests and responses.
/// Used for debugging, analytics, and cost tracking.
/// </summary>
public sealed class AiSuggestionLog: BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Request payload sent to AI (PII redacted).
    /// </summary>
    public string RequestPayload { get; set; } = null!;

    /// <summary>
    /// Response payload from AI. 
    /// </summary>
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// AI model used.
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Tokens consumed (for cost tracking).
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public int? ProcessingTimeMs { get; set; }

    /// <summary>
    /// Request was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
