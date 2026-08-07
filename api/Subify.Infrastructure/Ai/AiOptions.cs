namespace Subify.Infrastructure.Ai;

/// <summary>LLM client defaults (9.1). Bound from <c>Ai</c> config section.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>OpenAI-compatible API root (…/v1).</summary>
    public string DefaultBaseUrl { get; set; } = "https://api.openai.com/v1";

    public string DefaultModel { get; set; } = "gpt-4o-mini";

    public int HttpTimeoutSeconds { get; set; } = 60;

    /// <summary>App-level daily cap per user (9.2.4). Minute cap is ASP.NET rate limit.</summary>
    public int DailyLimit { get; set; } = 20;

    public double Temperature { get; set; } = 0.3;
}
