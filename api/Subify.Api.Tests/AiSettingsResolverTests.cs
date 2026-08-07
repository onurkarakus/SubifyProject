using Subify.Infrastructure.Ai;

namespace Subify.Api.Tests;

public class AiSettingsResolverTests
{
    [Theory]
    [InlineData("openai", null, "https://api.openai.com/v1")]
    [InlineData("groq", null, "https://api.groq.com/openai/v1")]
    [InlineData("gemini", null, "https://generativelanguage.googleapis.com/v1beta/openai")]
    [InlineData("google", null, "https://generativelanguage.googleapis.com/v1beta/openai")]
    [InlineData("xai", null, "https://api.x.ai/v1")]
    [InlineData("grok", null, "https://api.x.ai/v1")]
    [InlineData("openrouter", null, "https://openrouter.ai/api/v1")]
    [InlineData("deepseek", null, "https://api.deepseek.com/v1")]
    [InlineData("ollama", null, "http://localhost:11434/v1")]
    [InlineData("custom", null, "https://api.openai.com/v1")]
    [InlineData("custom", "http://my-proxy.local/v1/", "http://my-proxy.local/v1")]
    [InlineData("openai", "https://override.example/v1", "https://override.example/v1")]
    // Native Gemini path must not be used — client appends /chat/completions
    [InlineData("gemini", "https://generativelanguage.googleapis.com/v1beta/models/", "https://generativelanguage.googleapis.com/v1beta/openai")]
    [InlineData("gemini", "https://generativelanguage.googleapis.com/v1beta/models", "https://generativelanguage.googleapis.com/v1beta/openai")]
    [InlineData("gemini", "https://generativelanguage.googleapis.com/v1beta", "https://generativelanguage.googleapis.com/v1beta/openai")]
    public void ResolveBaseUrl_maps_presets_and_honors_explicit(
        string? provider,
        string? explicitBaseUrl,
        string expected)
    {
        var actual = AiSettingsResolver.ResolveBaseUrl(provider, explicitBaseUrl);
        Assert.Equal(expected, actual);
    }
}
