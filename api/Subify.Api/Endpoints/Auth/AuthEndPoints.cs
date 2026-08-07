using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Api.Common.RateLimiting;
using Subify.Application.Features.Auth.AcceptInvite;
using Subify.Application.Features.Auth.ChangePassword;
using Subify.Application.Features.Auth.ForgotPassword;
using Subify.Application.Features.Auth.Login;
using Subify.Application.Features.Auth.Logout;
using Subify.Application.Features.Auth.Refresh;
using Subify.Application.Features.Auth.Register;
using Subify.Application.Features.Auth.ResetPasswordWithToken;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Auth;

public class AuthEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
                [FromBody] LoginCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Ok(r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Login")
            .WithSummary("User login")
            .WithDescription(
                "Email/password → tokens + user summary. No EmailConfirmed check. " +
                "401 AUTH_001 invalid credentials; 423 AUTH_005 lockout.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();

        MapRefreshEndpoint(group, "/refresh-token", "RefreshToken");
        MapRefreshEndpoint(group, "/refresh", "RefreshTokenAlias");

        // 7.2.3 / 7.2.5 — public accept (works when public registration is off)
        group.MapPost("/accept-invite", async (
                [FromBody] AcceptInviteCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Ok(r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("AcceptInvite")
            .WithSummary("Accept invite and create account")
            .WithDescription(
                "Body: token + fullName + password. Creates User role. " +
                "Single-use + expiry enforced (AUTH_015). No public registration required.")
            .Produces<AcceptInviteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRateLimiting(RateLimitingOptions.RegisterPolicy)
            .AllowAnonymous();

        group.MapPost("/logout", async (
                [FromBody] LogoutCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Logout")
            .WithSummary("Logout / revoke refresh token")
            .WithDescription(
                "Body: { refreshToken } and/or { allSessions: true }. " +
                "Revokes with reason logout. Idempotent for unknown tokens. Task 3.2.4.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous(); // single-token logout without access JWT

        group.MapPost("/register", async (
                [FromBody] RegisterCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: r => Results.Created($"/api/users/{r.UserId}", r),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("Register")
            .WithSummary("User registration")
            .WithDescription(
                "Creates User + NotificationSettings. EmailConfirmed=true. " +
                "Blocked when setup incomplete or AllowPublicRegistration=false (403 AUTH_014). " +
                "Duplicate email 409 AUTH_008.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.RegisterPolicy)
            .AllowAnonymous();

        group.MapPost("/change-password", async (
                [FromBody] ChangePasswordCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("ChangePassword")
            .WithSummary("Change own password")
            .WithDescription("Requires auth. currentPassword + newPassword. Revokes all refresh sessions. Task 3.2.14.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(AuthPolicies.Authenticated);

        // 15.2.1 / 3.2.7 — forgot password (email when SMTP configured)
        group.MapPost("/forgot-password", async (
                [FromBody] ForgotPasswordCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("ForgotPassword")
            .WithSummary("Request password reset email")
            .WithDescription(
                "Always 204 for valid email format (no account enumeration). " +
                "Sends ResetPassword template when user exists and SMTP is configured. " +
                "No email confirm. Rate limited.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();

        // 15.2.1 / 3.2.8 — reset with token from email
        group.MapPost("/reset-password", async (
                [FromBody] ResetPasswordWithTokenCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("ResetPasswordWithToken")
            .WithSummary("Reset password with email token")
            .WithDescription(
                "Body: email + token + newPassword. Invalid/expired token → AUTH_009. " +
                "Revokes refresh sessions on success.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();

        // Admin password reset: AdminUserEndPoints POST /api/admin/users/{id}/reset-password (3.2.15 / 7.5.1)
    }

    private static void MapRefreshEndpoint(RouteGroupBuilder group, string route, string name)
    {
        group.MapPost(route, async (
                [FromBody] RefreshCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    onSuccess: response => Results.Ok(response),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName(name)
            .WithSummary("Refresh tokens (rotate)")
            .WithDescription(
                "Body: { refreshToken }. New access+refresh; old replaced. Reuse → AUTH_016. Tasks 3.1.3 / 3.2.3.")
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingOptions.LoginPolicy)
            .AllowAnonymous();
    }
}
