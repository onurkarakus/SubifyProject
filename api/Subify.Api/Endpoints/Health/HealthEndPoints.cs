using Subify.Api.Common.Abstractions;

namespace Subify.Api.Endpoints.Health;

public sealed class HealthEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthLiveResponse(
                Status: "Healthy",
                Timestamp: DateTimeOffset.UtcNow)))
            .WithTags("Health")
            .WithName("HealthLive")
            .WithSummary("Liveness probe")
            .WithDescription("Returns 200 when the API process is running. Used by container healthchecks; does not check database.")
            .Produces<HealthLiveResponse>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .DisableRateLimiting();
    }

    private sealed record HealthLiveResponse(string Status, DateTimeOffset Timestamp);
}
