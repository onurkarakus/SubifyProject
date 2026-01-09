using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;