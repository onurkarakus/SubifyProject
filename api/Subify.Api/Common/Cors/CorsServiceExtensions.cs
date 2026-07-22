namespace Subify.Api.Common.Cors;

public static class CorsServiceExtensions
{
    /// <summary>
    /// Registers CORS with origins from configuration.
    /// Development defaults to localhost:3000 when no origins are configured.
    /// Production requires explicit <c>Cors:AllowedOrigins</c>.
    /// </summary>
    public static IServiceCollection AddSubifyCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>() ?? new CorsOptions();

        var origins = corsOptions.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0 && environment.IsDevelopment())
        {
            origins =
            [
                "http://localhost:3000",
                "http://127.0.0.1:3000"
            ];
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    // No cross-origin browser clients allowed until configured
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

    public static IApplicationBuilder UseSubifyCors(this IApplicationBuilder app)
    {
        return app.UseCors(CorsOptions.PolicyName);
    }
}
