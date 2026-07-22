using Microsoft.EntityFrameworkCore;
using Subify.Api.Common.Abstractions;
using Subify.Infrastructure.Persistence;

namespace Subify.Api.Endpoints.Health;

public sealed class HealthEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (SubifyDbContext db, CancellationToken ct) =>
            {
                bool? setupComplete = null;
                try
                {
                    setupComplete = await db.SystemSettings
                        .AsNoTracking()
                        .Select(s => (bool?)s.IsSetupComplete)
                        .FirstOrDefaultAsync(ct);
                }
                catch
                {
                    // DB may be unavailable; liveness still OK
                }

                return Results.Ok(new HealthLiveResponse(
                    Status: "Healthy",
                    Timestamp: DateTimeOffset.UtcNow,
                    SetupRequired: setupComplete is false));
            })
            .WithTags("Health")
            .WithName("HealthLive")
            .WithSummary("Liveness probe")
            .WithDescription("Process up. Optional setupRequired when DB reachable (3S.1.5).")
            .Produces<HealthLiveResponse>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .DisableRateLimiting();
    }

    private sealed record HealthLiveResponse(
        string Status,
        DateTimeOffset Timestamp,
        bool? SetupRequired);
}

