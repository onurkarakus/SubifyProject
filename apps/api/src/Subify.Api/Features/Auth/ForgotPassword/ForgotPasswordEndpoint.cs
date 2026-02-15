using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Features.Auth.Logout;

namespace Subify.Api.Features.Auth.ForgotPassword
{
    public class ForgotPasswordEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/forgot-password", async (ISender sender, [FromBody] ForgotPasswordCommand command) =>
            {
                var result = await sender.Send(command);

                return result.MapResult(
                    onSuccess: () => Results.Ok(new { message = "Şifre sıfırlama linki e-posta adresinize gönderildi." }), // Assuming a success message is desired
                    onFailure: result => Results.Problem(result.ToProblemDetails())
                );
            })
            .WithTags("Auth")
            .WithName("ForgotPassword")
            .WithSummary("Forgot Password")
            .WithDescription("Sends a password reset link to the user's email address.")
            .WithOpenApi();
        }
    }
}
