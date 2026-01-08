using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.RefreshTokens;

public record RefreshTokensCommand(string AccessToken, string RefreshToken): IRequest<Result<RefreshTokensResponse>>;
