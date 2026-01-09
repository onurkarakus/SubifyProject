using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;