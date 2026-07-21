using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password): IRequest<Result<LoginResponse>>;