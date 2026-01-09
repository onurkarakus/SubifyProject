using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Auth.Register;

namespace Subify.Api.Features.Auth.ResetPassword;

public class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", async ([FromBody] RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }

            return Results.Ok(result.Value);
        })
            .WithTags("Auth")
            .WithName("ResetPassword")
            .WithSummary("Resets a user's password.")
            .WithDescription("Resets the password for a user using the provided email, token, and new password.")
            .Produces<RegisterResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}
