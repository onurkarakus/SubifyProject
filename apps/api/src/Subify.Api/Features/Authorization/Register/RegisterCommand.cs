using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Authorization.Register;

public record RegisterCommand(string Email, string Password, string FullName): IRequest<Result<RegisterResponse>>
{

}

public record RegisterResponse(string Email, string AccessToken, string RefreshToken, DateTime Expiration);
