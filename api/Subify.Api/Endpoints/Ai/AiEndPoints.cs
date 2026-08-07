using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.RateLimiting;
using Subify.Application.Features.Ai;
using Subify.Application.Features.Ai.AnalyzeSubscriptions;
using Subify.Application.Features.Ai.GetAiHistory;
using Subify.Application.Features.Ai.GetAiHistoryById;
using Subify.Application.Features.Ai.ReportCommentary;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Ai;

/// <summary>AI analyze + history (Faz 9). Requires instance LLM API key (BYOK).</summary>
public sealed class AiEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 9.2.1
        group.MapPost("/analyze", async (
                [FromBody] AnalyzeBody? body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new AnalyzeSubscriptionsCommand(
                        Lang: body?.Lang,
                        AcceptLanguage: httpContext.Request.Headers.AcceptLanguage.ToString()),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("AiAnalyze")
            .WithSummary("Analyze subscriptions with AI")
            .WithDescription(
                "Auth user. Needs ≥1 active subscription and SuperAdmin-configured LLM key. " +
                "Rate limit 5/min (middleware) + 20/day (app). Logs request/response. " +
                "AI_KEY_MISSING if no key.")
            .Produces<AiAnalyzeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting(RateLimitingOptions.AiPolicy);

        // Report period commentary (reports optional follow-up)
        group.MapPost("/report-commentary", async (
                [FromBody] ReportCommentaryBody? body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ReportCommentaryCommand(
                        Months: body?.Months ?? 6,
                        Lang: body?.Lang,
                        AcceptLanguage: httpContext.Request.Headers.AcceptLanguage.ToString()),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("AiReportCommentary")
            .WithSummary("AI commentary for period reports")
            .WithDescription(
                "Auth user. Builds narrative from monthly-spend series, categories, and budget. " +
                "Months 3–12. Same AI key + rate limits as analyze. Logs to AI history.")
            .Produces<AiReportCommentaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting(RateLimitingOptions.AiPolicy);

        // 9.2.2
        group.MapGet("/history", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetAiHistoryQuery(
                        Page: page ?? 1,
                        PageSize: pageSize ?? 10),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("AiHistory")
            .WithSummary("AI analysis history")
            .WithDescription("Own history only. Pagination page/pageSize.")
            .Produces<ListAiHistoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // History detail — full tips/summary for one log entry (own only)
        group.MapGet("/history/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetAiHistoryByIdQuery(id),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("AiHistoryById")
            .WithSummary("AI analysis history detail")
            .WithDescription("Returns full stored analyze payload for one entry. Own history only.")
            .Produces<AiHistoryDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public sealed record AnalyzeBody(string? Lang = null);

    public sealed record ReportCommentaryBody(int? Months = 6, string? Lang = null);
}
