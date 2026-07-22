using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Register;

public record RegisterCommand(string FullName, string Email, string Password) : IRequest<Result<RegisterResponse>>;