using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subify.Application.Common.Interfaces;
using Subify.Domain.Errors;
using Subify.Domain.Shared;
using Subify.Infrastructure.Persistence;

namespace Subify.Infrastructure.Ai;

/// <summary>Reads BYOK key/model/provider/base URL from SystemSettings (9.1.2).</summary>
public sealed class AiSettingsResolver : IAiSettingsResolver
{
    private readonly SubifyDbContext _db;
    private readonly AiOptions _options;

    public AiSettingsResolver(SubifyDbContext db, IOptions<AiOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<Result<AiRuntimeSettings>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null || string.IsNullOrWhiteSpace(settings.AiApiKey))
        {
            return Result.Failure<AiRuntimeSettings>(DomainErrors.AiErrors.ApiKeyMissing);
        }

        var model = string.IsNullOrWhiteSpace(settings.AiModel)
            ? _options.DefaultModel
            : settings.AiModel.Trim();

        var baseUrl = ResolveBaseUrl(settings.AiProvider, settings.AiBaseUrl);

        return Result.Success(new AiRuntimeSettings(
            ApiKey: settings.AiApiKey.Trim(),
            Model: model,
            BaseUrl: baseUrl,
            Provider: settings.AiProvider));
    }

    public const string GeminiOpenAiCompatibleBaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/openai";

    /// <summary>
    /// Explicit base URL wins (after normalization). Otherwise map known OpenAI-compatible presets.
    /// Unknown / custom without URL falls back to configured default (OpenAI).
    /// </summary>
    public static string ResolveBaseUrl(
        string? provider,
        string? explicitBaseUrl,
        string defaultBaseUrl = "https://api.openai.com/v1")
    {
        if (!string.IsNullOrWhiteSpace(explicitBaseUrl))
        {
            return NormalizeOpenAiCompatibleBaseUrl(explicitBaseUrl, provider);
        }

        return provider?.Trim().ToLowerInvariant() switch
        {
            "groq" => "https://api.groq.com/openai/v1",
            // Google AI Studio (OpenAI-compatible Gemini endpoint — NOT .../models/)
            "gemini" or "google" or "google-ai" => GeminiOpenAiCompatibleBaseUrl,
            // xAI Grok (not Groq)
            "xai" or "grok" => "https://api.x.ai/v1",
            "openrouter" => "https://openrouter.ai/api/v1",
            "deepseek" => "https://api.deepseek.com/v1",
            // Ollama OpenAI-compatible shim; override with AiBaseUrl for remote hosts
            "ollama" => "http://localhost:11434/v1",
            "openai" or null or "" or "custom" =>
                string.IsNullOrWhiteSpace(defaultBaseUrl)
                    ? "https://api.openai.com/v1"
                    : defaultBaseUrl.TrimEnd('/'),
            _ => string.IsNullOrWhiteSpace(defaultBaseUrl)
                ? "https://api.openai.com/v1"
                : defaultBaseUrl.TrimEnd('/')
        };
    }

    /// <summary>
    /// Google quickstart shows native <c>.../v1beta/models/</c> + generateContent.
    /// Subify talks OpenAI-compatible <c>.../v1beta/openai/chat/completions</c>.
    /// Auto-correct common paste mistakes so users don't get HTTP 404.
    /// </summary>
    public static string NormalizeOpenAiCompatibleBaseUrl(string baseUrl, string? provider = null)
    {
        var u = baseUrl.Trim().TrimEnd('/');
        var isGoogleHost = u.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);
        var isGeminiProvider = provider is not null &&
            (provider.Equals("gemini", StringComparison.OrdinalIgnoreCase)
             || provider.Equals("google", StringComparison.OrdinalIgnoreCase)
             || provider.Equals("google-ai", StringComparison.OrdinalIgnoreCase));

        if (isGoogleHost)
        {
            // Native: .../v1beta/models or .../v1beta/models/xxx → OpenAI compat root
            if (u.Contains("/models", StringComparison.OrdinalIgnoreCase)
                || !u.Contains("/openai", StringComparison.OrdinalIgnoreCase))
            {
                return GeminiOpenAiCompatibleBaseUrl;
            }
        }
        else if (isGeminiProvider
                 && (u.Contains("/models", StringComparison.OrdinalIgnoreCase)
                     || string.IsNullOrWhiteSpace(u)))
        {
            return GeminiOpenAiCompatibleBaseUrl;
        }

        return u;
    }

    private string ResolveBaseUrl(string? provider, string? explicitBaseUrl) =>
        ResolveBaseUrl(
            provider,
            explicitBaseUrl,
            string.IsNullOrWhiteSpace(_options.DefaultBaseUrl)
                ? "https://api.openai.com/v1"
                : _options.DefaultBaseUrl);
}
