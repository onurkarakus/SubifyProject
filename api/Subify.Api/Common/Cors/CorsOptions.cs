namespace Subify.Api.Common.Cors;

/// <summary>
/// CORS configuration bound from the <c>Cors</c> configuration section.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "SubifyCors";

    /// <summary>
    /// Allowed browser origins (e.g. Next.js web app).
    /// Env example: Cors__AllowedOrigins__0=https://app.example.com
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
