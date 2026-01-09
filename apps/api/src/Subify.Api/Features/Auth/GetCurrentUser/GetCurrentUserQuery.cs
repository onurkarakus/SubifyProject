using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery() : IRequest<Result<GetCurrentUserResponse>>;
