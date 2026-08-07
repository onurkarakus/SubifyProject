using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.ExchangeRates.GetExchangeRates;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.ExchangeRates;

/// <summary>Exchange rates from DB snapshots (Faz 6.2).</summary>
public sealed class ExchangeRateEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exchange-rates")
            .WithTags("ExchangeRates")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 6.2.3
        group.MapGet("/", async (
                [FromQuery] string? @base,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetExchangeRatesQuery(Base: @base),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("GetExchangeRates")
            .WithSummary("Latest exchange rates")
            .WithDescription(
                "Last snapshot for ?base= (default: user MainCurrency). " +
                "Serves last-known rates when live provider is down. On empty DB attempts one fetch.")
            .Produces<ExchangeRatesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
