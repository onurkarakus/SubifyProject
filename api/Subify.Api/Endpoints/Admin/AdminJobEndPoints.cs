using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Admin.Jobs.RunExchangeRateSync;
using Subify.Application.Features.Admin.Jobs.RunRenewalReminders;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Admin;

/// <summary>8.1 — SuperAdmin manual job triggers (ops).</summary>
public sealed class AdminJobEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/jobs")
            .WithTags("Admin · Jobs")
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapPost("/renewal-reminders/run", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new RunRenewalRemindersCommand(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("RunRenewalReminders")
            .WithSummary("Run renewal reminder scan now")
            .WithDescription(
                "SuperAdmin only. Same logic as EmailJobs background host (8.1 / 15.3.1). " +
                "Respects SMTP config, EmailEnabled prefs, daysBeforeRenewal, and dedupe (8.2/15.3.2). " +
                "Returns { processedCount }.")
            .Produces<RunRenewalRemindersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/exchange-rates/sync", async (
                [FromQuery] string? @base,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new RunExchangeRateSyncCommand(Base: @base),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("RunExchangeRateSync")
            .WithSummary("Force exchange-rate sync now")
            .WithDescription(
                "SuperAdmin only. Calls live FX provider for ?base= (default: instance DefaultCurrency), " +
                "persists snapshots, clears GET cache. Returns rates + success/fallback message.")
            .Produces<RunExchangeRateSyncResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
