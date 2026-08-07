using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>
/// OpenAI-compatible chat completions client (9.1.1).
/// Implementations must not log API keys.
/// </summary>
public interface IAiClient
{
    Task<Result<AiChatCompletionResult>> CompleteAsync(
        AiChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AiChatCompletionRequest(
    string ApiKey,
    string Model,
    string BaseUrl,
    IReadOnlyList<AiChatMessage> Messages,
    double Temperature = 0.3,
    /// <summary>When true, request OpenAI <c>response_format=json_object</c> (analyze).</summary>
    bool RequireJsonObjectResponse = false);

public sealed record AiChatMessage(string Role, string Content);

public sealed record AiChatCompletionResult(
    string Content,
    string? Model,
    int? PromptTokens,
    int? CompletionTokens);
