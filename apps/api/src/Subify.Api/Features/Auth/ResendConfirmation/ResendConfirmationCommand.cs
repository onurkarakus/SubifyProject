using MediatR;
using Subify.Domain.Shared;

namespace Subify.Api.Features.Auth.ResendConfirmation;

public record ResendConfirmationCommand(string Email, string Locale) : IRequest<Result>;
