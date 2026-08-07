using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;

namespace Subify.Infrastructure.Ai;

/// <summary>OpenAI-compatible chat completions HTTP client (9.1.1).</summary>
public sealed class OpenAiCompatibleClient : IAiClient
{
    public const string HttpClientName = "OpenAiCompatible";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiCompatibleClient> _logger;

    public OpenAiCompatibleClient(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<OpenAiCompatibleClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<AiChatCompletionResult>> CompleteAsync(
        AiChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Result.Failure<AiChatCompletionResult>(DomainErrors.AiErrors.ApiKeyMissing);
        }

        var baseUrl = request.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/chat/completions";
        var client = _httpClientFactory.CreateClient(HttpClientName);

        var body = new ChatRequestBody
        {
            Model = request.Model,
            Temperature = request.Temperature,
            // Analyze uses JSON mode; test-ai / plain prompts leave format unset.
            ResponseFormat = request.RequireJsonObjectResponse
                ? new ResponseFormatBody { Type = "json_object" }
                : null,
            Messages = request.Messages
                .Select(m => new MessageBody { Role = m.Role, Content = m.Content })
                .ToList()
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var providerMessage = ExtractProviderMessage(raw);
                _logger.LogWarning(
                    "AI provider HTTP {Status} at {Url} (model {Model}): {Message}",
                    (int)response.StatusCode,
                    url,
                    request.Model,
                    providerMessage ?? $"(body length {raw.Length})");

                if ((int)response.StatusCode is 401 or 403)
                {
                    return Result.Failure<AiChatCompletionResult>(DomainErrors.AiErrors.InvalidApiKey);
                }

                return Result.Failure<AiChatCompletionResult>(
                    DomainErrors.AiErrors.ProviderHttpError((int)response.StatusCode, providerMessage));
            }

            var parsed = JsonSerializer.Deserialize<ChatResponseBody>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var content = ExtractMessageContent(parsed?.Choices?.FirstOrDefault()?.Message);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning(
                    "AI provider returned empty content (model {Model}, body length {Len})",
                    request.Model,
                    raw.Length);
                return Result.Failure<AiChatCompletionResult>(DomainErrors.AiErrors.ProcessingError);
            }

            return Result.Success(new AiChatCompletionResult(
                Content: content,
                Model: parsed?.Model,
                PromptTokens: parsed?.Usage?.PromptTokens,
                CompletionTokens: parsed?.Usage?.CompletionTokens));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI provider request failed for {Url}", url);
            return Result.Failure<AiChatCompletionResult>(DomainErrors.AiErrors.ServiceUnavailable);
        }
    }

    /// <summary>OpenAI content is usually a string; some gateways return content parts.</summary>
    private static string? ExtractMessageContent(MessageBody? message)
    {
        if (message is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            return message.Content;
        }

        // Fallback: some serializers leave Content null if shape differs
        return null;
    }

    /// <summary>Best-effort extract of error.message from OpenAI/Gemini style error JSON.</summary>
    internal static string? ExtractProviderMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var msg) &&
                    msg.ValueKind == JsonValueKind.String)
                {
                    return Truncate(msg.GetString(), 300);
                }

                if (error.ValueKind == JsonValueKind.String)
                {
                    return Truncate(error.GetString(), 300);
                }
            }

            if (root.TryGetProperty("message", out var topMsg) &&
                topMsg.ValueKind == JsonValueKind.String)
            {
                return Truncate(topMsg.GetString(), 300);
            }
        }
        catch (JsonException)
        {
            // not JSON
        }

        var oneLine = raw.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return Truncate(oneLine, 200);
    }

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        s = s.Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }

    private sealed class ChatRequestBody
    {
        public string Model { get; set; } = "";
        public double Temperature { get; set; }
        public List<MessageBody> Messages { get; set; } = [];
        public ResponseFormatBody? ResponseFormat { get; set; }
    }

    private sealed class MessageBody
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private sealed class ResponseFormatBody
    {
        public string Type { get; set; } = "json_object";
    }

    private sealed class ChatResponseBody
    {
        public string? Model { get; set; }
        public List<ChoiceBody>? Choices { get; set; }
        public UsageBody? Usage { get; set; }
    }

    private sealed class ChoiceBody
    {
        public MessageBody? Message { get; set; }
    }

    private sealed class UsageBody
    {
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
    }
}
