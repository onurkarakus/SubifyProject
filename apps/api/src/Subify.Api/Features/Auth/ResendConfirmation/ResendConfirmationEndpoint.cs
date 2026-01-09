using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.ResendConfirmation;

public class ResendConfirmationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/resend-confirmation", async ([FromBody] ResendConfirmationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            if (result.IsFailure)
            {
                return result.ToProblemDetail();
            }
            return Results.Ok();
        })
            .WithTags("Auth")
            .WithName("ResendConfirmation")
            .WithSummary("Resends the email confirmation link to the user's email address.")
            .WithDescription("Sends a new email confirmation link to the specified email address if the user exists and their email is not yet confirmed.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}
