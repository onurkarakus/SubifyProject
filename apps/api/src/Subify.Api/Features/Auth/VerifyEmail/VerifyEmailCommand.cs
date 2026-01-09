using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.VerifyEmail;

public record VerifyEmailCommand(string UserId, string Token) : IRequest<Result>;