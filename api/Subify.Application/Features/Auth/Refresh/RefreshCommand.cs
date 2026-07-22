using MediatR;
using Subify.Domain.Shared;

namespace Subify.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<RefreshResponse>>;
