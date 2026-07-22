namespace Subify.Api.Common.RateLimiting;

/// <summary>
/// Rate limit settings bound from the <c>RateLimiting</c> configuration section.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const string LoginPolicy = "auth-login";
    public const string RegisterPolicy = "auth-register";
    public const string AiPolicy = "ai-analyze";

    public RateLimitWindowOptions Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitWindowOptions Register { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    public RateLimitWindowOptions Ai { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
}

public sealed class RateLimitWindowOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
