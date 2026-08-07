using Subify.Domain.Common;

namespace Subify.Domain.Entities;

/// <summary>Persisted AI analyze request/response for history (9.2.5).</summary>
public class AiSuggestionLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string RequestPayload { get; private set; } = string.Empty;
    public string ResponsePayload { get; private set; } = string.Empty;

    public ApplicationUser User { get; private set; } = null!;

    protected AiSuggestionLog() { }

    public AiSuggestionLog(Guid userId, string requestPayload, string responsePayload)
    {
        Id = GuidGenerator.NewId();
        CreatedAt = DateTimeOffset.UtcNow;
        UserId = userId;
        RequestPayload = requestPayload ?? string.Empty;
        ResponsePayload = responsePayload ?? string.Empty;
    }

    public static AiSuggestionLog Create(Guid userId, string requestPayload, string responsePayload) =>
        new(userId, requestPayload, responsePayload);
}