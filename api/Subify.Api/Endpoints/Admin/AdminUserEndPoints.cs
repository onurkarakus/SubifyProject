using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subify.Api.Common.Abstractions;
using Subify.Api.Common.Extensions;
using Subify.Application.Features.Admin.Users;
using Subify.Application.Features.Admin.Users.CreateAdminUser;
using Subify.Application.Features.Admin.Users.ListAdminUsers;
using Subify.Application.Features.Admin.Users.PatchAdminUser;
using Subify.Application.Features.Auth.AdminResetPassword;
using Subify.Domain.Constants;
using Subify.Infrastructure.Authorization;

namespace Subify.Api.Endpoints.Admin;

/// <summary>Admin user management (Faz 7.1 + 7.5 password reset bridge).</summary>
public sealed class AdminUserEndPoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin · Users");

        // 7.1.1
        group.MapGet("/", async (
                [FromQuery] string? search,
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new ListAdminUsersQuery(
                        Search: search,
                        Page: page ?? SubscriptionConstants.DefaultPage,
                        PageSize: pageSize ?? SubscriptionConstants.DefaultPageSize),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("ListAdminUsers")
            .WithSummary("List users")
            .WithDescription(
                "SuperAdmin/Admin. Paginated list + search (email/fullName). " +
                "Returns active subscription count only — not subscription entities (7.1.4).")
            .Produces<ListAdminUsersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(AuthPolicies.AdminOrAbove);

        // 7.1.2
        group.MapPost("/", async (
                [FromBody] CreateAdminUserCommand command,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.MapResult(
                    r => Results.Created($"/api/admin/users/{r.Id}", r),
                    httpContext.Request.Path.Value);
            })
            .WithName("CreateAdminUser")
            .WithSummary("Create user manually")
            .WithDescription(
                "SuperAdmin/Admin. Email + fullName + password. Role User (default) or Admin " +
                "(Admin role requires SuperAdmin caller). Never creates SuperAdmin.")
            .Produces<AdminUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.AdminOrAbove);

        // 7.1.3 / 7.1.5
        group.MapPatch("/{id:guid}", async (
                Guid id,
                [FromBody] PatchAdminUserBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new PatchAdminUserCommand(
                        UserId: id,
                        Role: body.Role,
                        IsLocked: body.IsLocked,
                        IsDisabled: body.IsDisabled,
                        FullName: body.FullName),
                    cancellationToken);
                return result.MapResult(r => Results.Ok(r), httpContext.Request.Path.Value);
            })
            .WithName("PatchAdminUser")
            .WithSummary("Update user (lock / disable / role)")
            .WithDescription(
                "SuperAdmin only. Lock/unlock, soft-disable, set role Admin|User. " +
                "SuperAdmin targets protected. Disabling revokes sessions and blocks login.")
            .Produces<AdminUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.SuperAdmin);

        // 7.5.1 / 3.2.15 — “Şifre sıfırla” from admin users table (UI calls this)
        group.MapPost("/{id:guid}/reset-password", async (
                Guid id,
                [FromBody] AdminResetPasswordBody body,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new AdminResetPasswordCommand(id, body.NewPassword),
                    cancellationToken);
                return result.MapResult(
                    onSuccess: () => Results.NoContent(),
                    instance: httpContext.Request.Path.Value);
            })
            .WithName("AdminResetPassword")
            .WithSummary("Reset user password")
            .WithDescription(
                "SuperAdmin only. Body: { newPassword }. No email. " +
                "Revokes target sessions + clears temporary lockout. " +
                "Cannot reset own password (use change-password). " +
                "Activity audit without password value. Tasks 3.2.15 / 7.5.1.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.SuperAdmin);
    }

    public sealed record PatchAdminUserBody(
        string? Role = null,
        bool? IsLocked = null,
        bool? IsDisabled = null,
        string? FullName = null);

    public sealed record AdminResetPasswordBody(string NewPassword);
}
