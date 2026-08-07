namespace Subify.Api.Common.Cors;

public static class CorsServiceExtensions
{
    private static readonly string[] DevDefaultOrigins =
    [
        "http://localhost:3000",
        "http://127.0.0.1:3000"
    ];

    /// <summary>
    /// Registers CORS with origins from configuration (14.1.3).
    /// Development / Testing: defaults to localhost:3000 when unset.
    /// Production / Staging: empty origins deny all browser cross-origin (must set Cors:AllowedOrigins).
    /// Never uses AllowAnyOrigin with credentials.
    /// </summary>
    public static IServiceCollection AddSubifyCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>() ?? new CorsOptions();

        var origins = NormalizeOrigins(corsOptions.AllowedOrigins);

        if (origins.Length == 0 && IsLooseEnvironment(environment))
        {
            origins = DevDefaultOrigins;
        }

        if (origins.Length == 0 && !IsLooseEnvironment(environment))
        {
            // Fail closed — browser SPA will not work until WEB_ORIGIN / Cors__AllowedOrigins is set.
            Console.Error.WriteLine(
                "[Subify CORS] No Cors:AllowedOrigins configured in {0}. " +
                "Browser clients are blocked. Set Cors__AllowedOrigins__0 to your web origin.",
                environment.EnvironmentName);
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>Used by tests — same normalization as runtime.</summary>
    public static string[] NormalizeOrigins(IEnumerable<string>? raw) =>
        (raw ?? [])
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin =>
            origin.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool IsLooseEnvironment(IHostEnvironment environment) =>
        environment.IsDevelopment()
        || environment.IsEnvironment("Testing");

    public static IApplicationBuilder UseSubifyCors(this IApplicationBuilder app)
    {
        return app.UseCors(CorsOptions.PolicyName);
    }
}
