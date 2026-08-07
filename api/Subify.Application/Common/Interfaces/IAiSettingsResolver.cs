using Subify.Domain.Shared;

namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Resolves BYOK LLM settings from SystemSettings (9.1.2).
/// Missing key → <c>AI_KEY_MISSING</c>.
/// </summary>
public interface IAiSettingsResolver
{
    Task<Result<AiRuntimeSettings>> ResolveAsync(CancellationToken cancellationToken = default);
}

public sealed record AiRuntimeSettings(
    string ApiKey,
    string Model,
    string BaseUrl,
    string? Provider);
