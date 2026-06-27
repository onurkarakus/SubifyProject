using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class AiSuggestionLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string RequestPayload { get; private set; } = string.Empty;
    public string ResponsePayload { get; private set; } = string.Empty;

    public ApplicationUser User { get; private set; } = null!;

    protected AiSuggestionLog() { }

    public AiSuggestionLog(Guid userId, string requestPayload, string responsePayload)
    {
        UserId = userId;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
    }
}