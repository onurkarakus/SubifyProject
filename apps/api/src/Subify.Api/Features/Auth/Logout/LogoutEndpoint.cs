using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;

namespace Subify.Api.Features.Auth.Logout;

public class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (ISender sender, [FromBody] LogoutCommand command) =>
        {
            var result = await sender.Send(command);
            return result.MapResult(
                onSuccess: () => Results.Ok(new { message = "Başarıyla çıkış yapıldı." }), // As per API_CONTRACTS.md
                onFailure: result => Results.Problem(result.ToProblemDetails())
            );
        })
        .WithTags("Auth")
        .WithDescription("Logs out the user by revoking the provided refresh token.")
        .WithDisplayName("Logout")
        .WithSummary("Logs out the user by revoking the provided refresh token.")
        .WithOpenApi();
    }
}