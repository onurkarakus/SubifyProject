using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.AdminResetPassword;

/// <summary>SuperAdmin sets another user's password without mail (task 3.2.15).</summary>
public sealed record AdminResetPasswordCommand(
    Guid UserId,
    string NewPassword) : IRequest<Result>;
