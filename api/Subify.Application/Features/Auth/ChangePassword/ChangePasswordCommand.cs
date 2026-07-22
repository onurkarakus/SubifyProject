using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.ChangePassword;

/// <summary>Authenticated user changes own password (task 3.2.14).</summary>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;
