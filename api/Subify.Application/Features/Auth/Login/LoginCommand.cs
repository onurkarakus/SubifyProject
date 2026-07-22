using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Login;

/// <summary>Login body (task 3.2.2).</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;