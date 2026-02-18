using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.UserCategories.CreateUserCategory;
using Subify.Api.Features.UserCategories.DeleteUserCategory;
using Subify.Api.Features.UserCategories.GetUserCategories;
using Subify.Api.Features.UserCategories.UpdateUserCategory;

namespace Subify.Api.Features.UserCategories;

public class UserCategoriesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-categories")
            .WithTags("Categories (User)")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserCategoriesQuery());

            return result.MapResult(
                onSuccess: categories => Results.Ok(categories),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .WithName("Get User Categories")
            .WithSummary("Get all user categories")
            .WithDescription("Get all user categories for the authenticated user")
            .Produces<List<GetUserCategoryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/", async (ISender sender, [FromBody] CreateUserCategoryCommand command) =>
        {
            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: createdUserId => Results.Ok(createdUserId),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .WithName("Create User Category")
            .WithSummary("Creates a new user category.")
            .WithDescription("Creates a new user category with the provided name, description, icon, and color.")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id}", async (ISender sender, [FromQuery] Guid id, [FromBody] DeleteUserCategoryCommand command) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .WithName("Delete User Category")
            .WithSummary("Deletes an existing user category.")
            .WithDescription("Deletes an existing user category by its ID.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id}", async (ISender sender, [FromQuery] Guid id, [FromBody] UpdateUserCategoryCommand command) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await sender.Send(command);

            return result.MapResult(
                onSuccess: () => Results.Ok(),
                onFailure: result => Results.Problem(result.ToProblemDetails()));
        })
            .WithName("Update User Category")
            .WithSummary("Updates an existing user category.")
            .WithDescription("Updates an existing user category. The ID in the URL must match the ID in the request body.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
