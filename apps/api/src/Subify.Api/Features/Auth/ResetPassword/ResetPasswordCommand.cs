using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;
