using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Register;

/// <summary>Public registration body (task 3.2.1).</summary>
public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password) : IRequest<Result<RegisterResponse>>;