using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Providers;
using Subify.Application.Features.Providers.Admin.CreateAdminProvider;
using Subify.Application.Features.Providers.Admin.DeleteAdminProvider;
using Subify.Application.Features.Providers.Admin.ImportAdminProviders;
using Subify.Application.Features.Providers.Admin.UpdateAdminProvider;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Providers;

/// <summary>SuperAdmin provider catalog management (5.2.3 / 16.6.3).</summary>
public sealed class AdminProviderEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/providers")
            .WithTags("Admin · Providers")
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapPost("/import", async (
                [FromBody] ImportAdminProvidersCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ImportAdminProviders")
            .WithSummary("Bulk import provider catalog")
            .WithDescription(
                "SuperAdmin only. Body: { providers: [...], updateExisting?: bool }. " +
                "By slug: create missing; skip existing unless updateExisting=true. Max 200.")
            .Produces<ImportAdminProvidersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
                [FromBody] CreateAdminProviderCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/providers/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateAdminProvider")
            .WithSummary("Create catalog provider")
            .WithDescription("SuperAdmin only. Duplicate slug/name → PROV_002/003.")
            .Produces<ProviderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateAdminProviderBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateAdminProviderCommand(
                    Id: id,
                    Name: body.Name,
                    Slug: body.Slug,
                    Currency: body.Currency,
                    BillingCycle: body.BillingCycle,
                    Region: body.Region,
                    Price: body.Price,
                    PriceBefore: body.PriceBefore,
                    SourceUrl: body.SourceUrl,
                    LogoUrl: body.LogoUrl,
                    IsActive: body.IsActive);

                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateAdminProvider")
            .WithSummary("Update catalog provider")
            .WithDescription("SuperAdmin only. Can reactivate soft-deleted providers (isActive=true).")
            .Produces<ProviderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new DeleteAdminProviderCommand(id), cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("DeleteAdminProvider")
            .WithSummary("Soft-delete catalog provider")
            .WithDescription("SuperAdmin only. 409 PROV_005 if active subscriptions reference it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public sealed record UpdateAdminProviderBody(
        string Name,
        string Slug,
        string Currency,
        string BillingCycle,
        string Region,
        decimal? Price = null,
        decimal? PriceBefore = null,
        string? SourceUrl = null,
        string? LogoUrl = null,
        bool IsActive = true);
}
