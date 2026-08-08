using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Resources;
using Subify.Application.Features.Resources.Admin.CreateAdminResource;
using Subify.Application.Features.Resources.Admin.DeleteAdminResource;
using Subify.Application.Features.Resources.Admin.ListAdminResources;
using Subify.Application.Features.Resources.Admin.UpdateAdminResource;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Resources;

/// <summary>SuperAdmin resource CRUD (Faz 6.3.3).</summary>
public sealed class AdminResourceEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/resources")
            .WithTags("Admin · Resources")
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        group.MapGet("/", async (
                [FromQuery] string? lang,
                [FromQuery] string? pageName,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListAdminResourcesQuery(Lang: lang, PageName: pageName),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListAdminResources")
            .WithSummary("List i18n resources")
            .WithDescription("SuperAdmin only. Optional lang and pageName filters.")
            .Produces<ListAdminResourcesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
                [FromBody] CreateAdminResourceCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/admin/resources/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateAdminResource")
            .WithSummary("Create i18n resource")
            .WithDescription("SuperAdmin only. Unique (pageName, name, languageCode). Invalidates pack cache.")
            .Produces<AdminResourceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateAdminResourceBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new UpdateAdminResourceCommand(
                        Id: id,
                        PageName: body.PageName,
                        Name: body.Name,
                        LanguageCode: body.LanguageCode,
                        Value: body.Value),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateAdminResource")
            .WithSummary("Update i18n resource")
            .WithDescription("SuperAdmin only. Sets UpdatedAt for delta sync; invalidates cache.")
            .Produces<AdminResourceResponse>(StatusCodes.Status200OK)
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
                var result = await mediator.Send(new DeleteAdminResourceCommand(id), cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("DeleteAdminResource")
            .WithSummary("Delete i18n resource")
            .WithDescription("SuperAdmin only. Hard delete + cache invalidate.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public sealed record UpdateAdminResourceBody(
        string PageName,
        string Name,
        string LanguageCode,
        string Value);
}
