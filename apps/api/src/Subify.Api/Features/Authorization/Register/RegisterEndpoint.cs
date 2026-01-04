using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Authorization.Register;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async ([FromBody] RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }

            return Results.Ok(result.Value);
        })
            .WithTags("Auth")
            .WithName("Register")
            .WithSummary("Registers a new user.")
            .WithDescription("Creates a new user account with the provided email, password, and full name.")
            .Produces<RegisterResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();
    }
}
