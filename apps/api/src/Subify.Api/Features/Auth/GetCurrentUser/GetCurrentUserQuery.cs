using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery(string UserId) : IRequest<Result<GetCurrentUserResponse>>;
