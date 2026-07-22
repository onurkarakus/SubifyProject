using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Common.Health;

public static class HealthCheckExtensions
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddSubifyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<SubifyDbContext>(
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag]);

        return services;
    }

    public static IEndpointConventionBuilder MapSubifyReadyHealthCheck(this IEndpointRouteBuilder app)
    {
        return app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
                ResponseWriter = WriteReadyResponseAsync,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            })
            .WithTags("Health")
            .WithName("HealthReady")
            .WithSummary("Readiness probe")
            .WithDescription("Returns 200 when PostgreSQL is reachable; 503 otherwise.")
            .AllowAnonymous()
            .DisableRateLimiting();
    }

    private static async Task WriteReadyResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var databaseEntry = report.Entries.TryGetValue("postgres", out var entry)
            ? entry
            : (HealthReportEntry?)null;

        var payload = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            database = new
            {
                status = databaseEntry?.Status.ToString() ?? "Unknown",
                description = databaseEntry?.Description,
                durationMs = databaseEntry?.Duration.TotalMilliseconds
            },
            totalDurationMs = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
