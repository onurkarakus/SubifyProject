using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Categories;
using Subify.Application.Features.Categories.CreateUserCategory;
using Subify.Application.Features.Categories.DeleteUserCategory;
using Subify.Application.Features.Categories.GetSystemCategories;
using Subify.Application.Features.Categories.GetUserCategories;
using Subify.Application.Features.Categories.UpdateUserCategory;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Categories;

/// <summary>System + user categories (Faz 5.1).</summary>
public sealed class CategoryEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 5.1.1
        group.MapGet("/", async (
                [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
                [FromQuery] string? locale,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetSystemCategoriesQuery(
                        AcceptLanguage: acceptLanguage,
                        ExplicitLocale: locale),
                    cancellationToken);

                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListSystemCategories")
            .WithSummary("List system categories")
            .WithDescription(
                "Active catalog categories ordered by sortOrder. " +
                "Name localized via Resources (Accept-Language, ?locale=, or user profile). Fallback: slug.")
            .Produces<ListCategoriesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 5.1.2 — map before any {id} routes
        group.MapGet("/user", async (
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetUserCategoriesQuery(), cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListUserCategories")
            .WithSummary("List user custom categories")
            .WithDescription("Only the current user's personal categories (soft-deleted excluded). Ordered by name.")
            .Produces<ListUserCategoriesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 5.1.3
        group.MapPost("/user", async (
                [FromBody] CreateUserCategoryCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/categories/user/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateUserCategory")
            .WithSummary("Create user custom category")
            .WithDescription("Body: name (required), icon, color. Duplicate name → 409 UCAT_004.")
            .Produces<UserCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 5.1.4
        group.MapPut("/user/{id:guid}", async (
                Guid id,
                [FromBody] UpdateUserCategoryBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new UpdateUserCategoryCommand(id, body.Name, body.Icon, body.Color),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("UpdateUserCategory")
            .WithSummary("Update user custom category")
            .WithDescription("Ownership required. 404 UCAT_001, 403 UCAT_002, duplicate name 409 UCAT_004.")
            .Produces<UserCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 5.1.5
        group.MapDelete("/user/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new DeleteUserCategoryCommand(id), cancellationToken);
                return result.MapResult(() => Results.NoContent(), httpContext.Request.Path.Value);
            })
            .WithName("DeleteUserCategory")
            .WithSummary("Delete user custom category")
            .WithDescription(
                "Soft-delete. 409 UCAT_003 if any active (non-archived) subscription uses it. " +
                "Ownership: 403 UCAT_002, missing 404 UCAT_001.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public sealed record UpdateUserCategoryBody(
        string Name,
        string? Icon = null,
        string? Color = null);
}


