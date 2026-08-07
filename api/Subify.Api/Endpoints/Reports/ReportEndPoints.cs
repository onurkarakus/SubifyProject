using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Reports;
using Subify.Application.Features.Reports.GetCategoryBreakdown;
using Subify.Application.Features.Reports.GetCurrencyDistribution;
using Subify.Application.Features.Reports.GetMonthlySpend;
using Subify.Application.Features.Reports.SendReportSummary;
using Subify.Domain.Constants;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Reports;

/// <summary>Reports (Faz 6.1) — no premium gate; own data only.</summary>
public sealed class ReportEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 6.1.1
        group.MapGet("/monthly-spend", async (
                [FromQuery] int? months,
                [FromQuery] string? currency,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetMonthlySpendQuery(
                        Months: months ?? ReportConstants.DefaultMonths,
                        Currency: currency),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetMonthlySpend")
            .WithSummary("Monthly spend chart")
            .WithDescription(
                "Last N months (default 12, max 36). Totals in MainCurrency or ?currency=. " +
                "No premium. Empty data + message when no subscriptions.")
            .Produces<MonthlySpendResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 6.1.2
        group.MapGet("/category-breakdown", async (
                [FromQuery] string? currency,
                [FromQuery] string? lang,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetCategoryBreakdownQuery(
                        AcceptLanguage: httpContext.Request.Headers.AcceptLanguage.ToString(),
                        ExplicitLocale: lang,
                        Currency: currency),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetCategoryBreakdown")
            .WithSummary("Category spend breakdown")
            .WithDescription(
                "Active subscriptions by category: total, percentage, count, color. " +
                "Empty data + message when none.")
            .Produces<CategoryBreakdownResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 6.1.3
        group.MapGet("/currency-distribution", async (
                [FromQuery] string? currency,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetCurrencyDistributionQuery(Currency: currency),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetCurrencyDistribution")
            .WithSummary("Currency distribution")
            .WithDescription(
                "Active subscriptions grouped by original currency. " +
                "MonthlyTotal in group currency; converted totals/percentage in MainCurrency.")
            .Produces<CurrencyDistributionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Email period summary (SMTP) — export follow-up
        group.MapPost("/email-summary", async (
                [FromBody] EmailSummaryBody? body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new SendReportSummaryCommand(
                        Months: body?.Months ?? 6,
                        Lang: body?.Lang,
                        AcceptLanguage: httpContext.Request.Headers.AcceptLanguage.ToString()),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("SendReportEmailSummary")
            .WithSummary("Email period spend summary")
            .WithDescription(
                "Sends ReportSummary template to the authenticated user's email. " +
                "Requires SMTP (SET_003 if missing). Months 3|6|12. " +
                "Dedupe: one successful send per user/day/months.")
            .Produces<SendReportSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    public sealed record EmailSummaryBody(int? Months = 6, string? Lang = null);
}
